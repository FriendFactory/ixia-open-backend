using System;

namespace Frever.Protobuf;

[AttributeUsage(AttributeTargets.Class)]
public class ProtobufInheritedFieldNumberAttribute : Attribute
{
    public ProtobufInheritedFieldNumberAttribute(int fieldNumber)
    {
        if (fieldNumber <= 0)
            throw new ArgumentException("Field number should be positive");
        FieldNumber = fieldNumber;
    }

    /// <summary>
    ///     Gets the field number for inherited data of the class.
    /// </summary>
    public int FieldNumber { get; private set; }
}