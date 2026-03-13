namespace Cutulu.Core;

using System.IO;
using System;

/// <summary>
/// Defines a generic base class for binary encoding, automatically registered by Cutulu.BinaryEncoding.
/// <para>
/// To make this encoder work, you need to implement a parameterless constructor that calls this constructor by it's encodable type.
/// </para>
/// <para>
/// To mute this encoder, add the DisableEncoder attribute to the class.
/// </para>
/// </summary>
public abstract class BinaryEncoder
{
    public readonly nint SourceHandle;
    public readonly Type SourceType;

    public BinaryEncoder(Type sourceType)
    {
        SourceHandle = (SourceType = sourceType).TypeHandle.Value;
    }

    public virtual int GetPriority() => 0;

    public abstract void Encode(BinaryWriter writer, Type type, object value);

    public abstract object Decode(BinaryReader reader, Type type);
}