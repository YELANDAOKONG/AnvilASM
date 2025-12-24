using Anvil.Constants;
using Anvil.Types;

namespace Anvil.Structures.ConstantPool;

// §4.4.1 CONSTANT_Class_info
public class CpClass : CpInfo
{
    public override ConstantPoolTag Tag => ConstantPoolTag.Class;
    public TUShort NameIndex { get; set; }

    public CpClass(TUShort nameIndex) => NameIndex = nameIndex;
    protected override void WriteInfo(Stream stream) => NameIndex.Write(stream);
    internal static CpClass ReadInfo(Stream stream) => new(TUShort.Read(stream));
}

// §4.4.3 CONSTANT_String_info
public class CpString : CpInfo
{
    public override ConstantPoolTag Tag => ConstantPoolTag.String;
    public TUShort StringIndex { get; set; }

    public CpString(TUShort stringIndex) => StringIndex = stringIndex;
    protected override void WriteInfo(Stream stream) => StringIndex.Write(stream);
    internal static CpString ReadInfo(Stream stream) => new(TUShort.Read(stream));
}

// §4.4.9 CONSTANT_MethodType_info
public class CpMethodType : CpInfo
{
    public override ConstantPoolTag Tag => ConstantPoolTag.MethodType;
    public TUShort DescriptorIndex { get; set; }

    public CpMethodType(TUShort descriptorIndex) => DescriptorIndex = descriptorIndex;
    protected override void WriteInfo(Stream stream) => DescriptorIndex.Write(stream);
    internal static CpMethodType ReadInfo(Stream stream) => new(TUShort.Read(stream));
}

// §4.4.11 CONSTANT_Module_info
public class CpModule : CpInfo
{
    public override ConstantPoolTag Tag => ConstantPoolTag.Module;
    public TUShort NameIndex { get; set; }

    public CpModule(TUShort nameIndex) => NameIndex = nameIndex;
    protected override void WriteInfo(Stream stream) => NameIndex.Write(stream);
    internal static CpModule ReadInfo(Stream stream) => new(TUShort.Read(stream));
}

// §4.4.12 CONSTANT_Package_info
public class CpPackage : CpInfo
{
    public override ConstantPoolTag Tag => ConstantPoolTag.Package;
    public TUShort NameIndex { get; set; }

    public CpPackage(TUShort nameIndex) => NameIndex = nameIndex;
    protected override void WriteInfo(Stream stream) => NameIndex.Write(stream);
    internal static CpPackage ReadInfo(Stream stream) => new(TUShort.Read(stream));
}
