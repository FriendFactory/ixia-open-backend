using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.AdminService.Core;

public static class ServicesCollectionExtensions
{
    //make AddTrivialPerEntityService a bit easy to use
    public static void AddTrivialPerEntityService<T>(
        this IServiceCollection services,
        Type genericServiceInterface,
        Type genericServiceImplementation
    )
    {
        AddTrivialPerEntityService<T>(services, genericServiceInterface.GetTypeInfo(), genericServiceImplementation.GetTypeInfo());
    }

    /// <summary>
    ///     Registers resolution of <paramref name="genericServiceInterface" />
    ///     to <paramref name="genericServiceImplementation" />
    ///     for all entities doesn't have other implementation registered before.
    /// </summary>
    public static void AddTrivialPerEntityService<T>(
        this IServiceCollection services,
        TypeInfo genericServiceInterface,
        TypeInfo genericServiceImplementation
    )
    {
        ValidateTypes(services, genericServiceInterface, genericServiceImplementation);

        var targetEntities = GetAllEntityTypes<T, Song>();
        foreach (var entityType in targetEntities)
        {
            var entityServiceInterface = genericServiceInterface.MakeGenericType(entityType);
            var resolvedService = services.FirstOrDefault(descriptor => entityServiceInterface.IsAssignableFrom(descriptor.ServiceType));

            if (resolvedService != null)
                continue;

            var entityServiceImplType = genericServiceImplementation.MakeGenericType(entityType);
            services.AddScoped(entityServiceInterface, entityServiceImplType);
        }
    }

    /// <summary>
    ///     Inject multiple implementation of type genericServiceInterfaceType
    /// </summary>
    /// <typeparam name="T">Main interface/type used to filter entities</typeparam>
    /// <param name="services"></param>
    /// <param name="genericServiceInterfaceType">interface/type to inject</param>
    /// <param name="genericServiceImplementationType">interface/Type implementation</param>
    public static void AddMultiplePerEntityService<T>(
        this IServiceCollection services,
        Type genericServiceInterfaceType,
        Type genericServiceImplementationType
    )
    {
        var genericServiceInterface = genericServiceInterfaceType.GetTypeInfo();
        var genericServiceImplementation = genericServiceImplementationType.GetTypeInfo();

        ValidateTypes(services, genericServiceInterface, genericServiceImplementation);

        var targetEntities = GetAllEntityTypes<T, Song>();
        foreach (var entityType in targetEntities)
        {
            var entityServiceInterface = genericServiceInterface.MakeGenericType(entityType);
            var entityServiceImplType = genericServiceImplementation.MakeGenericType(entityType);
            services.AddScoped(entityServiceInterface, entityServiceImplType);
        }
    }

    private static void ValidateTypes(IServiceCollection services, TypeInfo genericServiceInterface, TypeInfo genericServiceImplementation)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(genericServiceInterface);
        ArgumentNullException.ThrowIfNull(genericServiceImplementation);

        if (!genericServiceInterface.IsGenericType)
            throw new ArgumentException($"{nameof(genericServiceInterface)} should be open generic type.", nameof(genericServiceInterface));

        if (!genericServiceImplementation.IsGenericType)
            throw new ArgumentException(
                $"{nameof(genericServiceImplementation)} should be open generic type.",
                nameof(genericServiceImplementation)
            );

        if (genericServiceImplementation.IsAbstract)
            throw new ArgumentException(
                $"{nameof(genericServiceImplementation)} should not be abstract type.",
                nameof(genericServiceImplementation)
            );

        if (!genericServiceImplementation.IsClass)
            throw new ArgumentException($"{nameof(genericServiceImplementation)} should be class.", nameof(genericServiceImplementation));
    }

    /// <summary>
    ///     Gets all entity classes from data models assembly.
    /// </summary>
    public static IEnumerable<TypeInfo> GetAllEntityTypes<T, TDbModelType>()
    {
        return typeof(TDbModelType).Assembly.GetExportedTypes()
                                   .Select(t => t.GetTypeInfo())
                                   .Where(t => t.IsClass)
                                   .Where(t => t.ImplementedInterfaces.Contains(typeof(T)) || typeof(T).IsAssignableFrom(t));
    }
}