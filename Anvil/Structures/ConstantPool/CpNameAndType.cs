using Anvil.Constants;
using Anvil.Types;

namespace Anvil.Structures.ConstantPool;

public class CpNameAndType : CpInfo
{
    public override ConstantPoolTag Tag => ConstantPoolTag.NameAndType;
    public TUShort NameIndex { get; set; }
    public TUShort DescriptorIndex { get; set; }

    public CpNameAndType(TUShort nameIndex, TUShort descriptorIndex)
    {
        NameIndex = nameIndex;
        DescriptorIndex = descriptorIndex;
    }

    protected override void WriteInfo(Stream stream)
    {
        NameIndex.Write(stream);
        DescriptorIndex.Write(stream);
    }

    internal static CpNameAndType ReadInfo(Stream stream) 
        => new(TUShort.Read(stream), TUShort.Read(stream));
}