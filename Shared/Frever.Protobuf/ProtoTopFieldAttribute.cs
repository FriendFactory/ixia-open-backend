using System;

namespace Frever.Protobuf;

/// <summary>
///     Manages the order of the property in auto-generated protobuf contract.
///     Puts field at start of protobuf contract.
///     That allows to safely deserialize object with subset of properties.
///     Use it to add version field to base class and then deserialize base class.
///     It allows to check the version without potential errors on deserialized future fields.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ProtoTopFieldAttribute(int order) : Attribute
{
    public int Order { get; } = order;
}