using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Frever.Protobuf;

public static class ProtobufConvert
{
    private static readonly object TypeRegistrationSyncRoot = new object();
    private static readonly ISet<string> RegisteredTypes = new HashSet<string>(StringComparer.Ordinal);
    private static readonly StringComparer PropertyNameComparer = StringComparer.Create(CultureInfo.GetCultureInfo("en-US"), false);

    public static byte[] SerializeObject<T>(T input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        lock (TypeRegistrationSyncRoot)
        {
            if (!(input is string) && (input is IEnumerable en))
            {
                foreach (var item in en)
                    RegisterType(item.GetType());
            }

            var inputType = input.GetType();

            RegisterType(inputType);

            if (inputType.IsGenericType)
                foreach (var p in inputType.GetGenericArguments())
                    RegisterType(p);
        }


        using var ms = new MemoryStream();

        Serializer.Serialize(ms, input);

        return ms.ToArray();
    }


    public static T DeserializeObject<T>(byte[] input, params Type[] extraTypes)
    {
        lock (TypeRegistrationSyncRoot)
        {
            RegisterType(typeof(T));

            if (extraTypes != null)
                foreach (var type in extraTypes)
                    RegisterType(type);
        }

        using var ms = new MemoryStream(input);

        return Serializer.Deserialize<T>(ms);
    }

    public static object DeserializeObject(Type type, byte[] input, params Type[] extraTypes)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        lock (TypeRegistrationSyncRoot)
        {
            RegisterType(type);

            if (extraTypes != null)
                foreach (var extra in extraTypes)
                    RegisterType(extra);
        }

        using var ms = new MemoryStream(input);

        return Serializer.Deserialize(type, ms);
    }

    private static void RegisterType(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (type == typeof(object))
            return;

        if (!RegisteredTypes.Add(type.AssemblyQualifiedName))
            return;

        if (IsCollection(type, out var elementType))
        {
            RegisterType(elementType);

            return;
        }

        if (type.IsEnum)
        {
            RuntimeTypeModel.Default.Add(type, true);

            return;
        }

        if (IsSystemType(type))
            return;

        var addedTypes = RuntimeTypeModel.Default.GetTypes();

        foreach (var thing in addedTypes)
        {
            if (thing is MetaType metaType && metaType.Type.FullName == type.FullName)
                return;
        }

        var config = RuntimeTypeModel.Default.Add(type, false);

        var fieldStartNumber = 10; //seems like 1 is reserved, so just in case we start from 10

        var allProps = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                            .Where(prop => prop.CanRead && prop.CanWrite)
                            .Where(prop => !prop.IsDefined(typeof(ProtobufIgnoreAttribute), true))
                            .ToArray();

        var topProps = allProps.Where(p => p.IsDefined(typeof(ProtoTopFieldAttribute)))
                                .OrderBy(
                                    p =>
                                    {
                                        var attr = p.GetCustomAttribute<ProtoTopFieldAttribute>()!;
                                        return attr.Order;
                                    }
                                )
                                .ThenBy(p => p.Name, PropertyNameComparer);

        var noCustomOrderProps = allProps
                                .Where(
                                        p => !p.IsDefined(typeof(ProtoNewFieldAttribute)) && !p.IsDefined(typeof(ProtoTopFieldAttribute))
                                    )
                                .OrderBy(p => p.Name, PropertyNameComparer);

        var customOrderProps = allProps.Where(p => p.IsDefined(typeof(ProtoNewFieldAttribute)))
                                        .OrderBy(
                                            p =>
                                            {
                                                var attr = p.GetCustomAttribute<ProtoNewFieldAttribute>()!;
                                                return attr.Order;
                                            }
                                        )
                                        .ThenBy(p => p.Name, PropertyNameComparer);

        foreach (var prop in topProps.Concat(noCustomOrderProps).Concat(customOrderProps))
        {
            if (IsDictionaryOfObjects(prop.PropertyType))
                continue;

            RegisterType(prop.PropertyType);

            config.Add(fieldStartNumber, prop.Name, null);
            fieldStartNumber += 1;
        }

        var knownTypes = type.GetCustomAttribute<ProtobufKnownInheritorsAttribute>();

        if (knownTypes?.KnownInheritedTypes != null)
        {
            foreach (var t in knownTypes.KnownInheritedTypes.OrderByDescending(t => t.FullName, PropertyNameComparer))
                RegisterType(t);
        }

        var subtypesFieldsStartNumber = 100;
        var pointedSubtypesFieldStartNumber = 200;
        if (!type.IsValueType && type.BaseType != typeof(object) && !type.BaseType.Namespace.StartsWith(typeof(string).Namespace))
        {
            RegisterType(type.BaseType);
            var baseTypeConfig = RuntimeTypeModel.Default[type.BaseType];

            var nextSubtypeNumber = Attribute.IsDefined(type, typeof(ProtobufInheritedFieldNumberAttribute))
                                        ? pointedSubtypesFieldStartNumber +
                                            type.GetCustomAttribute<ProtobufInheritedFieldNumberAttribute>()!.FieldNumber
                                        : subtypesFieldsStartNumber + (baseTypeConfig.GetSubtypes()?.Length ?? 0) + 1;
            baseTypeConfig.AddSubType(nextSubtypeNumber, type);
        }
    }

    private static bool IsDictionaryOfObjects(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        var interfaces = type.GetInterfaces().Append(type).ToArray();

        var dictInterfaces = interfaces.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>)).ToArray();

        return dictInterfaces.Any(i => i.IsGenericType && i.GetGenericArguments()[1] == typeof(object));
    }

    private static bool IsCollection(Type type, out Type elementType)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        elementType = null;

        if (type == typeof(String))
            return false;

        if (type.IsArray)
        {
            elementType = type.GetElementType();

            return true;
        }

        var dictionary = type.GetInterfaces()
                                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        if (dictionary != null)
        {
            elementType = dictionary.GetGenericArguments().Last();

            return true;
        }

        var enumerable = type.GetInterfaces()
                                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerable != null)
        {
            elementType = enumerable.GetGenericArguments().First();

            return true;
        }

        return false;
    }

    private static bool IsSystemType(Type type)
    {
        return type.Namespace.StartsWith("System");
    }
}