using Anvil.Constants;
using Anvil.Types;

namespace Anvil.Structures.ConstantPool;

// §4.4.2 CONSTANT_Fieldref_info
public class CpFieldRef : CpInfo
{
    public override ConstantPoolTag Tag => ConstantPoolTag.Fieldref;
    public TUShort ClassIndex { get; set; }
    public TUShort NameAndTypeIndex { get; set; }

    public CpFieldRef(TUShort classIndex, TUShort nameAndTypeIndex)
    {
        ClassIndex = classIndex;
        NameAndTypeIndex = nameAndTypeIndex;
    }

    protected override void WriteInfo(Stream stream)
    {
        ClassIndex.Write(stream);
        NameAndTypeIndex.Write(stream);
    }

    internal static CpFieldRef ReadInfo(Stream stream) 
        => new(TUShort.Read(stream), TUShort.Read(stream));
}

// §4.4.2 CONSTANT_Methodref_info
public class CpMethodRef : CpInfo
{
    public override ConstantPoolTag Tag => ConstantPoolTag.Methodref;
    public TUShort ClassIndex { get; set; }
    public TUShort NameAndTypeIndex { get; set; }

    public CpMethodRef(TUShort classIndex, TUShort nameAndTypeIndex)
    {
        ClassIndex = classIndex;
        NameAndTypeIndex = nameAndTypeIndex;
    }

    protected override void WriteInfo(Stream stream)
    {
        ClassIndex.Write(stream);
        NameAndTypeIndex.Write(stream);
    }

    internal static CpMethodRef ReadInfo(Stream stream) 
        => new(TUShort.Read(stream), TUShort.Read(stream));
}

// §4.4.2 CONSTANT_InterfaceMethodref_info
public class CpInterfaceMethodRef : CpInfo
{
    public override ConstantPoolTag Tag => ConstantPoolTag.InterfaceMethodref;
    public TUShort ClassIndex { get; set; }
    public TUShort NameAndTypeIndex { get; set; }

    public CpInterfaceMethodRef(TUShort classIndex, TUShort nameAndTypeIndex)
    {
        ClassIndex = classIndex;
        NameAndTypeIndex = nameAndTypeIndex;
    }

    protected override void WriteInfo(Stream stream)
    {
        ClassIndex.Write(stream);
        NameAndTypeIndex.Write(stream);
    }

    internal static CpInterfaceMethodRef ReadInfo(Stream stream) 
        => new(TUShort.Read(stream), TUShort.Read(stream));
}
