using Anvil.Constants.Flags;
using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures;

/// <summary>
/// Represents a method in the class.
/// Spec §4.6: method_info
/// </summary>
public class MethodInfo : IStructure<MethodInfo>
{
    public MethodAccessFlags AccessFlags { get; set; }
    public TUShort NameIndex { get; set; }
    public TUShort DescriptorIndex { get; set; }
    public TUShort AttributesCount { get; set; }
    public AttributeInfo[] Attributes { get; set; } = Array.Empty<AttributeInfo>();

    public void Write(Stream stream)
    {
        new TUShort((ushort)AccessFlags).Write(stream);
        NameIndex.Write(stream);
        DescriptorIndex.Write(stream);
        
        new TUShort((ushort)Attributes.Length).Write(stream);
        foreach (var attr in Attributes) attr.Write(stream);
    }

    public static MethodInfo Read(Stream stream)
    {
        var method = new MethodInfo();
        method.AccessFlags = (MethodAccessFlags)TUShort.Read(stream).Value;
        method.NameIndex = TUShort.Read(stream);
        method.DescriptorIndex = TUShort.Read(stream);
        
        method.AttributesCount = TUShort.Read(stream);
        int count = method.AttributesCount.Value;
        method.Attributes = new AttributeInfo[count];
        for (int i = 0; i < count; i++)
        {
            method.Attributes[i] = AttributeInfo.Read(stream);
        }
        return method;
    }
}