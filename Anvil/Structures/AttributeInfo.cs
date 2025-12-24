using Anvil.Factories;
using Anvil.Interfaces;
using Anvil.Structures.ConstantPool;
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
    
    /// <summary>
    /// Attempts to parse the raw Info bytes into a strongly-typed structure.
    /// </summary>
    /// <param name="constantPool">The constant pool to resolve the attribute name.</param>
    /// <returns>The parsed body interface, or null if the attribute is unknown.</returns>
    public IAttribute? ResolveBody(CpInfo?[] constantPool)
    {
        if (AttributeNameIndex.Value == 0 || AttributeNameIndex.Value >= constantPool.Length)
        {
            return null;
        }

        var entry = constantPool[AttributeNameIndex.Value];
        if (entry is not CpUtf8 utf8Name)
        {
            return null;
        }

        return AttributeFactory.Create(utf8Name.Value, Info);
    }
}