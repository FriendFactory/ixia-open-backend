using System;

namespace Frever.AdminService.Api.Infrastructure;

[AttributeUsage(AttributeTargets.Class)]
public class ReplaceEntityControllerAttribute(Type entityType) : Attribute
{
    public Type EntityType { get; } = entityType ?? throw new ArgumentNullException(nameof(entityType));
}