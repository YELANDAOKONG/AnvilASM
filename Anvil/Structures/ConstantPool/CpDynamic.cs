using Anvil.Constants;
using Anvil.Types;

namespace Anvil.Structures.ConstantPool;

// §4.4.8 CONSTANT_MethodHandle_info
public class CpMethodHandle : CpInfo
{
    public override ConstantPoolTag Tag => ConstantPoolTag.MethodHandle;
    
    /// <summary>
    /// Value 1-9, denotes the kind of method handle.
    /// </summary>
    public TUByte ReferenceKind { get; set; }
    public TUShort ReferenceIndex { get; set; }

    public CpMethodHandle(TUByte referenceKind, TUShort referenceIndex)
    {
        ReferenceKind = referenceKind;
        ReferenceIndex = referenceIndex;
    }

    protected override void WriteInfo(Stream stream)
    {
        ReferenceKind.Write(stream);
        ReferenceIndex.Write(stream);
    }

    internal static CpMethodHandle ReadInfo(Stream stream) 
        => new(TUByte.Read(stream), TUShort.Read(stream));
}

// §4.4.10 CONSTANT_Dynamic_info
public class CpDynamic : CpInfo
{
    public override ConstantPoolTag Tag => ConstantPoolTag.Dynamic;
    public TUShort BootstrapMethodAttrIndex { get; set; }
    public TUShort NameAndTypeIndex { get; set; }

    public CpDynamic(TUShort bootstrapMethodAttrIndex, TUShort nameAndTypeIndex)
    {
        BootstrapMethodAttrIndex = bootstrapMethodAttrIndex;
        NameAndTypeIndex = nameAndTypeIndex;
    }

    protected override void WriteInfo(Stream stream)
    {
        BootstrapMethodAttrIndex.Write(stream);
        NameAndTypeIndex.Write(stream);
    }

    internal static CpDynamic ReadInfo(Stream stream) 
        => new(TUShort.Read(stream), TUShort.Read(stream));
}

// §4.4.10 CONSTANT_InvokeDynamic_info
public class CpInvokeDynamic : CpInfo
{
    public override ConstantPoolTag Tag => ConstantPoolTag.InvokeDynamic;
    public TUShort BootstrapMethodAttrIndex { get; set; }
    public TUShort NameAndTypeIndex { get; set; }

    public CpInvokeDynamic(TUShort bootstrapMethodAttrIndex, TUShort nameAndTypeIndex)
    {
        BootstrapMethodAttrIndex = bootstrapMethodAttrIndex;
        NameAndTypeIndex = nameAndTypeIndex;
    }

    protected override void WriteInfo(Stream stream)
    {
        BootstrapMethodAttrIndex.Write(stream);
        NameAndTypeIndex.Write(stream);
    }

    internal static CpInvokeDynamic ReadInfo(Stream stream) 
        => new(TUShort.Read(stream), TUShort.Read(stream));
}
