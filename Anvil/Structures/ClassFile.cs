using Anvil.Constants;
using Anvil.Constants.Flags;
using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures;

/// <summary>
/// Represents the low-level physical layout of a Java Class File.
/// Spec §4.1
/// </summary>
public class ClassFile : IStructure<ClassFile>
{
    public const uint MagicNumber = 0xCAFEBABE;

    public TUInt Magic { get; set; }
    public TUShort MinorVersion { get; set; }
    public TUShort MajorVersion { get; set; }
    
    public TUShort ConstantPoolCount { get; set; }
    /// <summary>
    /// The constant pool array.
    /// Note: Index 0 is unused (null). Long/Double entries consume two slots.
    /// </summary>
    public CpInfo?[] ConstantPool { get; set; } = Array.Empty<CpInfo>();

    public ClassAccessFlags AccessFlags { get; set; } // Strong Type
    public TUShort ThisClass { get; set; }
    public TUShort SuperClass { get; set; }
    
    public TUShort InterfacesCount { get; set; }
    public TUShort[] Interfaces { get; set; } = Array.Empty<TUShort>();

    public TUShort FieldsCount { get; set; }
    public FieldInfo[] Fields { get; set; } = Array.Empty<FieldInfo>();

    public TUShort MethodsCount { get; set; }
    public MethodInfo[] Methods { get; set; } = Array.Empty<MethodInfo>();

    public TUShort AttributesCount { get; set; }
    public AttributeInfo[] Attributes { get; set; } = Array.Empty<AttributeInfo>();

    public void Write(Stream stream)
    {
        Magic.Write(stream);
        MinorVersion.Write(stream);
        MajorVersion.Write(stream);

        // Constant Pool Size logic
        new TUShort((ushort)ConstantPool.Length).Write(stream);
        for (int i = 1; i < ConstantPool.Length; i++)
        {
            var entry = ConstantPool[i];
            if (entry == null) continue; // Skip holes
            entry.Write(stream);
        }

        new TUShort((ushort)AccessFlags).Write(stream); // Enum -> TUShort
        ThisClass.Write(stream);
        SuperClass.Write(stream);

        new TUShort((ushort)Interfaces.Length).Write(stream);
        foreach (var iface in Interfaces) iface.Write(stream);

        new TUShort((ushort)Fields.Length).Write(stream);
        foreach (var field in Fields) field.Write(stream);

        new TUShort((ushort)Methods.Length).Write(stream);
        foreach (var method in Methods) method.Write(stream);

        new TUShort((ushort)Attributes.Length).Write(stream);
        foreach (var attr in Attributes) attr.Write(stream);
    }

    public static ClassFile Read(Stream stream)
    {
        var cf = new ClassFile();

        cf.Magic = TUInt.Read(stream);
        if (cf.Magic.Value != MagicNumber)
            throw new FormatException($"Invalid Magic Number: 0x{cf.Magic.Value:X8}");

        cf.MinorVersion = TUShort.Read(stream);
        cf.MajorVersion = TUShort.Read(stream);

        // --- Read Constant Pool ---
        cf.ConstantPoolCount = TUShort.Read(stream);
        int cpLen = cf.ConstantPoolCount.Value;
        cf.ConstantPool = new CpInfo?[cpLen];
        
        for (int i = 1; i < cpLen; i++)
        {
            var entry = CpInfo.Read(stream);
            cf.ConstantPool[i] = entry;

            if (entry.Tag == ConstantPoolTag.Long || entry.Tag == ConstantPoolTag.Double)
            {
                i++; // Skip next index
            }
        }

        cf.AccessFlags = (ClassAccessFlags)TUShort.Read(stream).Value; // TUShort -> Enum
        cf.ThisClass = TUShort.Read(stream);
        cf.SuperClass = TUShort.Read(stream);

        // --- Read Interfaces ---
        cf.InterfacesCount = TUShort.Read(stream);
        cf.Interfaces = new TUShort[cf.InterfacesCount.Value];
        for (int i = 0; i < cf.Interfaces.Length; i++)
        {
            cf.Interfaces[i] = TUShort.Read(stream);
        }

        // --- Read Fields ---
        cf.FieldsCount = TUShort.Read(stream);
        cf.Fields = new FieldInfo[cf.FieldsCount.Value];
        for (int i = 0; i < cf.Fields.Length; i++)
        {
            cf.Fields[i] = FieldInfo.Read(stream);
        }

        // --- Read Methods ---
        cf.MethodsCount = TUShort.Read(stream);
        cf.Methods = new MethodInfo[cf.MethodsCount.Value];
        for (int i = 0; i < cf.Methods.Length; i++)
        {
            cf.Methods[i] = MethodInfo.Read(stream);
        }

        // --- Read Attributes ---
        cf.AttributesCount = TUShort.Read(stream);
        cf.Attributes = new AttributeInfo[cf.AttributesCount.Value];
        for (int i = 0; i < cf.Attributes.Length; i++)
        {
            cf.Attributes[i] = AttributeInfo.Read(stream);
        }

        return cf;
    }
}
