using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes;

/// <summary>
/// Represents the LineNumberTable attribute (§4.7.12).
/// </summary>
public class LineNumberTableAttribute : IStructure<LineNumberTableAttribute>, IAttribute
{
    public TUShort LineNumberTableLength { get; set; }
    public LineNumberTableEntry[] LineNumberTable { get; set; } = Array.Empty<LineNumberTableEntry>();

    public void Write(Stream stream)
    {
        new TUShort((ushort)LineNumberTable.Length).Write(stream);
        foreach (var entry in LineNumberTable)
        {
            entry.Write(stream);
        }
    }

    public static LineNumberTableAttribute Read(Stream stream)
    {
        var attr = new LineNumberTableAttribute();
        attr.LineNumberTableLength = TUShort.Read(stream);
        attr.LineNumberTable = new LineNumberTableEntry[attr.LineNumberTableLength.Value];
        
        for (int i = 0; i < attr.LineNumberTable.Length; i++)
        {
            attr.LineNumberTable[i] = LineNumberTableEntry.Read(stream);
        }
        return attr;
    }
}

public class LineNumberTableEntry : IStructure<LineNumberTableEntry>
{
    public TUShort StartPc { get; set; }
    public TUShort LineNumber { get; set; }

    public void Write(Stream stream)
    {
        StartPc.Write(stream);
        LineNumber.Write(stream);
    }

    public static LineNumberTableEntry Read(Stream stream)
    {
        return new LineNumberTableEntry
        {
            StartPc = TUShort.Read(stream),
            LineNumber = TUShort.Read(stream)
        };
    }
}