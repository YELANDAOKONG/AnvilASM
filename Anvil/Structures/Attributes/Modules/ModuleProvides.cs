using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes.Modules;

public class ModuleProvides : IStructure<ModuleProvides>
{
    public TUShort ProvidesIndex { get; set; }
    public TUShort ProvidesWithCount { get; set; }
    public TUShort[] ProvidesWithIndex { get; set; } = Array.Empty<TUShort>();

    public void Write(Stream stream)
    {
        ProvidesIndex.Write(stream);
        new TUShort((ushort)ProvidesWithIndex.Length).Write(stream);
        foreach (var idx in ProvidesWithIndex) idx.Write(stream);
    }

    public static ModuleProvides Read(Stream stream)
    {
        var prov = new ModuleProvides();
        prov.ProvidesIndex = TUShort.Read(stream);
        prov.ProvidesWithCount = TUShort.Read(stream);
        prov.ProvidesWithIndex = new TUShort[prov.ProvidesWithCount.Value];
        for (int i = 0; i < prov.ProvidesWithIndex.Length; i++) prov.ProvidesWithIndex[i] = TUShort.Read(stream);
        return prov;
    }
}