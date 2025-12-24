using Anvil.Interfaces;
using Anvil.Structures.Attributes.StackMap;
using Anvil.Types;

namespace Anvil.Structures.Attributes;

/// <summary>
/// Represents the StackMapTable attribute (§4.7.4).
/// </summary>
public class StackMapTableAttribute : IAttribute
{
    public StackMapFrame[] Entries { get; set; }

    public StackMapTableAttribute(StackMapFrame[] entries)
    {
        Entries = entries;
    }

    public void Write(Stream stream)
    {
        new TUShort((ushort)Entries.Length).Write(stream);
        foreach (var entry in Entries)
        {
            entry.Write(stream);
        }
    }

    public static StackMapTableAttribute Read(Stream stream)
    {
        var numberOfEntries = TUShort.Read(stream).Value;
        var entries = new StackMapFrame[numberOfEntries];
        
        for (int i = 0; i < numberOfEntries; i++)
        {
            entries[i] = StackMapFrame.Read(stream);
        }

        return new StackMapTableAttribute(entries);
    }
}