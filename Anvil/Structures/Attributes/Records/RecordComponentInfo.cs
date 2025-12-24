using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes.Records;

public class RecordComponentInfo : IStructure<RecordComponentInfo>
{
    public TUShort NameIndex { get; set; }
    public TUShort DescriptorIndex { get; set; }
    public TUShort AttributesCount { get; set; }
    public AttributeInfo[] Attributes { get; set; } = Array.Empty<AttributeInfo>();

    public void Write(Stream stream)
    {
        NameIndex.Write(stream);
        DescriptorIndex.Write(stream);
        new TUShort((ushort)Attributes.Length).Write(stream);
        foreach (var attr in Attributes) attr.Write(stream);
    }

    public static RecordComponentInfo Read(Stream stream)
    {
        var comp = new RecordComponentInfo();
        comp.NameIndex = TUShort.Read(stream);
        comp.DescriptorIndex = TUShort.Read(stream);
        
        comp.AttributesCount = TUShort.Read(stream);
        comp.Attributes = new AttributeInfo[comp.AttributesCount.Value];
        for (int i = 0; i < comp.Attributes.Length; i++)
        {
            comp.Attributes[i] = AttributeInfo.Read(stream);
        }
        return comp;
    }
}