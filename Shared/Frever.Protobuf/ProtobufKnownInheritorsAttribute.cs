using System;

namespace Frever.Protobuf;

[AttributeUsage(AttributeTargets.Class)]
public class ProtobufKnownInheritorsAttribute(params Type[] knownInheritedTypes) : Attribute
{
    public Type[] KnownInheritedTypes { get; private set; } = knownInheritedTypes ?? throw new ArgumentNullException(nameof(knownInheritedTypes));
}