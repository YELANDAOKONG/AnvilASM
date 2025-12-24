using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes.TypeAnnotations;

/// <summary>
/// Represents the target_info union (§4.7.20.1).
/// </summary>
public abstract class TargetInfo : IStructure<TargetInfo>
{
    public abstract void Write(Stream stream);

    public static TargetInfo Read(Stream stream) => throw new InvalidOperationException("Use Read(Stream, byte) instead.");

    public static TargetInfo Read(Stream stream, byte targetType)
    {
        return targetType switch
        {
            0x00 or 0x01 => TypeParameterTarget.Read(stream),
            0x10 => SupertypeTarget.Read(stream),
            0x11 or 0x12 => TypeParameterBoundTarget.Read(stream),
            0x13 or 0x14 or 0x15 => new EmptyTarget(),
            0x16 => FormalParameterTarget.Read(stream),
            0x17 => ThrowsTarget.Read(stream),
            0x40 or 0x41 => LocalvarTarget.Read(stream),
            0x42 => CatchTarget.Read(stream),
            0x43 or 0x44 or 0x45 or 0x46 => OffsetTarget.Read(stream),
            0x47 or 0x48 or 0x49 or 0x4A or 0x4B => TypeArgumentTarget.Read(stream),
            _ => throw new FormatException($"Unknown target_type: 0x{targetType:X2}")
        };
    }
}

public class TypeParameterTarget : TargetInfo
{
    public TUByte TypeParameterIndex { get; set; }
    public override void Write(Stream stream) => TypeParameterIndex.Write(stream);
    public new static TypeParameterTarget Read(Stream stream) => new() { TypeParameterIndex = TUByte.Read(stream) };
}

public class SupertypeTarget : TargetInfo
{
    public TUShort SupertypeIndex { get; set; }
    public override void Write(Stream stream) => SupertypeIndex.Write(stream);
    public new static SupertypeTarget Read(Stream stream) => new() { SupertypeIndex = TUShort.Read(stream) };
}

public class TypeParameterBoundTarget : TargetInfo
{
    public TUByte TypeParameterIndex { get; set; }
    public TUByte BoundIndex { get; set; }
    public override void Write(Stream stream) { TypeParameterIndex.Write(stream); BoundIndex.Write(stream); }
    public new static TypeParameterBoundTarget Read(Stream stream) => new() { TypeParameterIndex = TUByte.Read(stream), BoundIndex = TUByte.Read(stream) };
}

public class EmptyTarget : TargetInfo
{
    public override void Write(Stream stream) { } // No data
}

public class FormalParameterTarget : TargetInfo
{
    public TUByte FormalParameterIndex { get; set; }
    public override void Write(Stream stream) => FormalParameterIndex.Write(stream);
    public new static FormalParameterTarget Read(Stream stream) => new() { FormalParameterIndex = TUByte.Read(stream) };
}

public class ThrowsTarget : TargetInfo
{
    public TUShort ThrowsTypeIndex { get; set; }
    public override void Write(Stream stream) => ThrowsTypeIndex.Write(stream);
    public new static ThrowsTarget Read(Stream stream) => new() { ThrowsTypeIndex = TUShort.Read(stream) };
}

public class LocalvarTarget : TargetInfo
{
    public TUShort TableLength { get; set; }
    public LocalvarTableEntry[] Table { get; set; } = Array.Empty<LocalvarTableEntry>();

    public override void Write(Stream stream)
    {
        new TUShort((ushort)Table.Length).Write(stream);
        foreach (var entry in Table) entry.Write(stream);
    }

    public new static LocalvarTarget Read(Stream stream)
    {
        var target = new LocalvarTarget();
        target.TableLength = TUShort.Read(stream);
        target.Table = new LocalvarTableEntry[target.TableLength.Value];
        for (int i = 0; i < target.Table.Length; i++) target.Table[i] = LocalvarTableEntry.Read(stream);
        return target;
    }
}

public class LocalvarTableEntry : IStructure<LocalvarTableEntry>
{
    public TUShort StartPc { get; set; }
    public TUShort Length { get; set; }
    public TUShort Index { get; set; }

    public void Write(Stream stream) { StartPc.Write(stream); Length.Write(stream); Index.Write(stream); }
    public static LocalvarTableEntry Read(Stream stream) => new() { StartPc = TUShort.Read(stream), Length = TUShort.Read(stream), Index = TUShort.Read(stream) };
}

public class CatchTarget : TargetInfo
{
    public TUShort ExceptionTableIndex { get; set; }
    public override void Write(Stream stream) => ExceptionTableIndex.Write(stream);
    public new static CatchTarget Read(Stream stream) => new() { ExceptionTableIndex = TUShort.Read(stream) };
}

public class OffsetTarget : TargetInfo
{
    public TUShort Offset { get; set; }
    public override void Write(Stream stream) => Offset.Write(stream);
    public new static OffsetTarget Read(Stream stream) => new() { Offset = TUShort.Read(stream) };
}

public class TypeArgumentTarget : TargetInfo
{
    public TUShort Offset { get; set; }
    public TUByte TypeArgumentIndex { get; set; }
    public override void Write(Stream stream) { Offset.Write(stream); TypeArgumentIndex.Write(stream); }
    public new static TypeArgumentTarget Read(Stream stream) => new() { Offset = TUShort.Read(stream), TypeArgumentIndex = TUByte.Read(stream) };
}
