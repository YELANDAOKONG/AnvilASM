using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes;

/// <summary>
/// Represents the LocalVariableTypeTable attribute (§4.7.14).
/// </summary>
public class LocalVariableTypeTableAttribute : IStructure<LocalVariableTypeTableAttribute>, IAttribute
{
    public TUShort LocalVariableTypeTableLength { get; set; }
    public LocalVariableTypeTableEntry[] LocalVariableTypeTable { get; set; } = Array.Empty<LocalVariableTypeTableEntry>();

    public void Write(Stream stream)
    {
        new TUShort((ushort)LocalVariableTypeTable.Length).Write(stream);
        foreach (var entry in LocalVariableTypeTable)
        {
            entry.Write(stream);
        }
    }

    public static LocalVariableTypeTableAttribute Read(Stream stream)
    {
        var attr = new LocalVariableTypeTableAttribute();
        attr.LocalVariableTypeTableLength = TUShort.Read(stream);
        attr.LocalVariableTypeTable = new LocalVariableTypeTableEntry[attr.LocalVariableTypeTableLength.Value];

        for (int i = 0; i < attr.LocalVariableTypeTable.Length; i++)
        {
            attr.LocalVariableTypeTable[i] = LocalVariableTypeTableEntry.Read(stream);
        }
        return attr;
    }
}

public class LocalVariableTypeTableEntry : IStructure<LocalVariableTypeTableEntry>
{
    public TUShort StartPc { get; set; }
    public TUShort Length { get; set; }
    public TUShort NameIndex { get; set; }
    public TUShort SignatureIndex { get; set; }
    public TUShort Index { get; set; }

    public void Write(Stream stream)
    {
        StartPc.Write(stream);
        Length.Write(stream);
        NameIndex.Write(stream);
        SignatureIndex.Write(stream);
        Index.Write(stream);
    }

    public static LocalVariableTypeTableEntry Read(Stream stream)
    {
        return new LocalVariableTypeTableEntry
        {
            StartPc = TUShort.Read(stream),
            Length = TUShort.Read(stream),
            NameIndex = TUShort.Read(stream),
            SignatureIndex = TUShort.Read(stream),
            Index = TUShort.Read(stream)
        };
    }
}
