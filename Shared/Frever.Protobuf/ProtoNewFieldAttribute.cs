using System;

namespace Frever.Protobuf;

/// <summary>
///     Manages the order of the property in auto-generated protobuf contract.
///     Use it on adding new property to published contracts with any Order greater than any existing attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ProtoNewFieldAttribute(int order) : Attribute
{
    public int Order { get; } = order;
}