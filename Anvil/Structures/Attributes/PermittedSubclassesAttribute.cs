using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes;

public class PermittedSubclassesAttribute : IStructure<PermittedSubclassesAttribute>, IAttribute
{
    public TUShort NumberOfClasses { get; set; }
    public TUShort[] Classes { get; set; } = Array.Empty<TUShort>();

    public void Write(Stream stream)
    {
        new TUShort((ushort)Classes.Length).Write(stream);
        foreach (var cls in Classes) cls.Write(stream);
    }

    public static PermittedSubclassesAttribute Read(Stream stream)
    {
        var attr = new PermittedSubclassesAttribute();
        attr.NumberOfClasses = TUShort.Read(stream);
        attr.Classes = new TUShort[attr.NumberOfClasses.Value];
        for (int i = 0; i < attr.Classes.Length; i++)
        {
            attr.Classes[i] = TUShort.Read(stream);
        }
        return attr;
    }
}