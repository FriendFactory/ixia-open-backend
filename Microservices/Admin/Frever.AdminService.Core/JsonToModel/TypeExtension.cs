using System;
using System.Collections.Generic;
using System.Linq;

namespace Frever.AdminService.Core.JsonToModel;

internal static class TypeExtension
{
    public static bool IsCollection(this Type type)
    {
        return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    }
}