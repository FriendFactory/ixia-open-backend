using System;
using System.Collections.Generic;

namespace Frever.AdminService.Core.Services.EntityServices;

public class CallContext(object originalEntity, WriteOperation operation)
{
    private readonly Dictionary<Type, object> _features = new();

    public object OriginalEntity { get; } = originalEntity ?? throw new ArgumentNullException(nameof(originalEntity));

    public WriteOperation Operation { get; } = operation;

    /// <summary>
    ///     Context stores a set of custom data called feature to pass data between services and stages.
    ///     You could get and set features by type and set it.
    ///     Feature is created on request. Only feature modification possible.
    /// </summary>
    public T GetFeature<T>()
        where T : class, new()
    {
        if (typeof(T).Assembly == typeof(string).Assembly || typeof(T).Assembly.FullName.StartsWith("System.", StringComparison.Ordinal))
            throw new ArgumentException("You should use custom type as feature to prevent collisions.");

        if (!_features.ContainsKey(typeof(T)))
            _features[typeof(T)] = new T();

        return (T) _features[typeof(T)];
    }

    /// <summary>
    ///     Modify feature in context.
    ///     Feature in context is replaced with value returned by modifyFeature function.
    ///     Method returns modified feature.
    /// </summary>
    public T ModifyFeature<T>(Func<T, T> modifyFeature)
        where T : class, new()
    {
        _features.TryGetValue(typeof(T), out var existingFeature);

        var newFeature = modifyFeature((T) existingFeature ?? new T());
        if (newFeature == null)
            _features.Remove(typeof(T));
        else
            _features[typeof(T)] = newFeature;

        return newFeature;
    }
}

public enum WriteOperation
{
    Create,
    Update,
    Delete
}