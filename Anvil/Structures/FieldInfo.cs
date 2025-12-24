using Anvil.Constants.Flags;
using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures;

/// <summary>
/// Represents a field in the class.
/// Spec §4.5: field_info
/// </summary>
public class FieldInfo : IStructure<FieldInfo>
{
    public FieldAccessFlags AccessFlags { get; set; }
    public TUShort NameIndex { get; set; }
    public TUShort DescriptorIndex { get; set; }
    public TUShort AttributesCount { get; set; }
    public AttributeInfo[] Attributes { get; set; } = Array.Empty<AttributeInfo>();

    public void Write(Stream stream)
    {
        // Enum -> ushort -> TUShort -> Write
        new TUShort((ushort)AccessFlags).Write(stream);
        NameIndex.Write(stream);
        DescriptorIndex.Write(stream);
        
        new TUShort((ushort)Attributes.Length).Write(stream);
        foreach (var attr in Attributes) attr.Write(stream);
    }

    public static FieldInfo Read(Stream stream)
    {
        var field = new FieldInfo();
        // Read TUShort -> ushort -> Enum
        field.AccessFlags = (FieldAccessFlags)TUShort.Read(stream).Value;
        field.NameIndex = TUShort.Read(stream);
        field.DescriptorIndex = TUShort.Read(stream);
        
        field.AttributesCount = TUShort.Read(stream);
        int count = field.AttributesCount.Value;
        field.Attributes = new AttributeInfo[count];
        for (int i = 0; i < count; i++)
        {
            field.Attributes[i] = AttributeInfo.Read(stream);
        }
        return field;
    }
}