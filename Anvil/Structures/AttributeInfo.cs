using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures;

/// <summary>
/// Represents the raw structure of an attribute.
/// Spec §4.7: attribute_info { u2 attribute_name_index; u4 attribute_length; u1 info[attribute_length]; }
/// </summary>
public class AttributeInfo : IStructure<AttributeInfo>
{
    public TUShort AttributeNameIndex { get; set; }
    public TUInt AttributeLength { get; set; }
    public byte[] Info { get; set; }

    public AttributeInfo()
    {
        Info = Array.Empty<byte>();
    }

    public void Write(Stream stream)
    {
        AttributeNameIndex.Write(stream);
        // Ensure length matches the actual data
        new TUInt((uint)Info.Length).Write(stream); 
        stream.Write(Info);
    }

    public static AttributeInfo Read(Stream stream)
    {
        var attr = new AttributeInfo();
        attr.AttributeNameIndex = TUShort.Read(stream);
        attr.AttributeLength = TUInt.Read(stream);
        
        // Strictly read raw bytes. No interpretation here.
        attr.Info = new byte[attr.AttributeLength.Value];
        stream.ReadExactly(attr.Info);
        
        return attr;
    }
}