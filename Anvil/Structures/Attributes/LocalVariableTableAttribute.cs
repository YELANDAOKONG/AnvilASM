using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes;

/// <summary>
/// Represents the LocalVariableTable attribute (§4.7.13).
/// </summary>
public class LocalVariableTableAttribute : IStructure<LocalVariableTableAttribute>, IAttribute
{
    public TUShort LocalVariableTableLength { get; set; }
    public LocalVariableTableEntry[] LocalVariableTable { get; set; } = Array.Empty<LocalVariableTableEntry>();

    public void Write(Stream stream)
    {
        new TUShort((ushort)LocalVariableTable.Length).Write(stream);
        foreach (var entry in LocalVariableTable)
        {
            entry.Write(stream);
        }
    }

    public static LocalVariableTableAttribute Read(Stream stream)
    {
        var attr = new LocalVariableTableAttribute();
        attr.LocalVariableTableLength = TUShort.Read(stream);
        attr.LocalVariableTable = new LocalVariableTableEntry[attr.LocalVariableTableLength.Value];

        for (int i = 0; i < attr.LocalVariableTable.Length; i++)
        {
            attr.LocalVariableTable[i] = LocalVariableTableEntry.Read(stream);
        }
        return attr;
    }
}

public class LocalVariableTableEntry : IStructure<LocalVariableTableEntry>
{
    public TUShort StartPc { get; set; }
    public TUShort Length { get; set; }
    public TUShort NameIndex { get; set; }
    public TUShort DescriptorIndex { get; set; }
    public TUShort Index { get; set; }

    public void Write(Stream stream)
    {
        StartPc.Write(stream);
        Length.Write(stream);
        NameIndex.Write(stream);
        DescriptorIndex.Write(stream);
        Index.Write(stream);
    }

    public static LocalVariableTableEntry Read(Stream stream)
    {
        return new LocalVariableTableEntry
        {
            StartPc = TUShort.Read(stream),
            Length = TUShort.Read(stream),
            NameIndex = TUShort.Read(stream),
            DescriptorIndex = TUShort.Read(stream),
            Index = TUShort.Read(stream)
        };
    }
}
