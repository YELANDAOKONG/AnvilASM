using Anvil.Constants.Flags;
using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes;

public class InnerClassesAttribute : IStructure<InnerClassesAttribute>, IAttribute
{
    public TUShort NumberOfClasses { get; set; }
    public InnerClassInfo[] Classes { get; set; } = Array.Empty<InnerClassInfo>();

    public void Write(Stream stream)
    {
        new TUShort((ushort)Classes.Length).Write(stream);
        foreach (var cls in Classes) cls.Write(stream);
    }

    public static InnerClassesAttribute Read(Stream stream)
    {
        var attr = new InnerClassesAttribute();
        attr.NumberOfClasses = TUShort.Read(stream);
        attr.Classes = new InnerClassInfo[attr.NumberOfClasses.Value];
        for (int i = 0; i < attr.Classes.Length; i++)
        {
            attr.Classes[i] = InnerClassInfo.Read(stream);
        }
        return attr;
    }
}

public class InnerClassInfo : IStructure<InnerClassInfo>
{
    public TUShort InnerClassInfoIndex { get; set; }
    public TUShort OuterClassInfoIndex { get; set; }
    public TUShort InnerNameIndex { get; set; }
    public InnerClassAccessFlags InnerClassAccessFlags { get; set; } // Strong Type

    public void Write(Stream stream)
    {
        InnerClassInfoIndex.Write(stream);
        OuterClassInfoIndex.Write(stream);
        InnerNameIndex.Write(stream);
        new TUShort((ushort)InnerClassAccessFlags).Write(stream);
    }

    public static InnerClassInfo Read(Stream stream)
    {
        return new InnerClassInfo
        {
            InnerClassInfoIndex = TUShort.Read(stream),
            OuterClassInfoIndex = TUShort.Read(stream),
            InnerNameIndex = TUShort.Read(stream),
            InnerClassAccessFlags = (InnerClassAccessFlags)TUShort.Read(stream).Value
        };
    }
}