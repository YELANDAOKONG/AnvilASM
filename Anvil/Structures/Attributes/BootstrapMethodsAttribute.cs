using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes;

public class BootstrapMethodsAttribute : IStructure<BootstrapMethodsAttribute>, IAttribute
{
    public TUShort NumBootstrapMethods { get; set; }
    public BootstrapMethod[] BootstrapMethods { get; set; } = Array.Empty<BootstrapMethod>();

    public void Write(Stream stream)
    {
        new TUShort((ushort)BootstrapMethods.Length).Write(stream);
        foreach (var bm in BootstrapMethods) bm.Write(stream);
    }

    public static BootstrapMethodsAttribute Read(Stream stream)
    {
        var attr = new BootstrapMethodsAttribute();
        attr.NumBootstrapMethods = TUShort.Read(stream);
        attr.BootstrapMethods = new BootstrapMethod[attr.NumBootstrapMethods.Value];
        for (int i = 0; i < attr.BootstrapMethods.Length; i++)
        {
            attr.BootstrapMethods[i] = BootstrapMethod.Read(stream);
        }
        return attr;
    }
}

public class BootstrapMethod : IStructure<BootstrapMethod>
{
    public TUShort BootstrapMethodRef { get; set; }
    public TUShort NumBootstrapArguments { get; set; }
    public TUShort[] BootstrapArguments { get; set; } = Array.Empty<TUShort>();

    public void Write(Stream stream)
    {
        BootstrapMethodRef.Write(stream);
        new TUShort((ushort)BootstrapArguments.Length).Write(stream);
        foreach (var arg in BootstrapArguments) arg.Write(stream);
    }

    public static BootstrapMethod Read(Stream stream)
    {
        var bm = new BootstrapMethod();
        bm.BootstrapMethodRef = TUShort.Read(stream);
        bm.NumBootstrapArguments = TUShort.Read(stream);
        bm.BootstrapArguments = new TUShort[bm.NumBootstrapArguments.Value];
        for (int i = 0; i < bm.BootstrapArguments.Length; i++)
        {
            bm.BootstrapArguments[i] = TUShort.Read(stream);
        }
        return bm;
    }
}