using Anvil.Constants.Flags;
using Anvil.Interfaces;
using Anvil.Structures.Attributes.Modules;
using Anvil.Types;

namespace Anvil.Structures.Attributes;

public class ModuleAttribute : IStructure<ModuleAttribute>, IAttribute
{
    public TUShort ModuleNameIndex { get; set; }
    public ModuleFlags ModuleFlags { get; set; }
    public TUShort ModuleVersionIndex { get; set; }

    public TUShort RequiresCount { get; set; }
    public ModuleRequires[] Requires { get; set; } = Array.Empty<ModuleRequires>();

    public TUShort ExportsCount { get; set; }
    public ModuleExports[] Exports { get; set; } = Array.Empty<ModuleExports>();

    public TUShort OpensCount { get; set; }
    public ModuleOpens[] Opens { get; set; } = Array.Empty<ModuleOpens>();

    public TUShort UsesCount { get; set; }
    public TUShort[] UsesIndex { get; set; } = Array.Empty<TUShort>();

    public TUShort ProvidesCount { get; set; }
    public ModuleProvides[] Provides { get; set; } = Array.Empty<ModuleProvides>();

    public void Write(Stream stream)
    {
        ModuleNameIndex.Write(stream);
        new TUShort((ushort)ModuleFlags).Write(stream);
        ModuleVersionIndex.Write(stream);

        new TUShort((ushort)Requires.Length).Write(stream);
        foreach (var req in Requires) req.Write(stream);

        new TUShort((ushort)Exports.Length).Write(stream);
        foreach (var exp in Exports) exp.Write(stream);

        new TUShort((ushort)Opens.Length).Write(stream);
        foreach (var opn in Opens) opn.Write(stream);

        new TUShort((ushort)UsesIndex.Length).Write(stream);
        foreach (var use in UsesIndex) use.Write(stream);

        new TUShort((ushort)Provides.Length).Write(stream);
        foreach (var prov in Provides) prov.Write(stream);
    }

    public static ModuleAttribute Read(Stream stream)
    {
        var mod = new ModuleAttribute();
        mod.ModuleNameIndex = TUShort.Read(stream);
        mod.ModuleFlags = (ModuleFlags)TUShort.Read(stream).Value;
        mod.ModuleVersionIndex = TUShort.Read(stream);

        mod.RequiresCount = TUShort.Read(stream);
        mod.Requires = new ModuleRequires[mod.RequiresCount.Value];
        for (int i = 0; i < mod.Requires.Length; i++) mod.Requires[i] = ModuleRequires.Read(stream);

        mod.ExportsCount = TUShort.Read(stream);
        mod.Exports = new ModuleExports[mod.ExportsCount.Value];
        for (int i = 0; i < mod.Exports.Length; i++) mod.Exports[i] = ModuleExports.Read(stream);

        mod.OpensCount = TUShort.Read(stream);
        mod.Opens = new ModuleOpens[mod.OpensCount.Value];
        for (int i = 0; i < mod.Opens.Length; i++) mod.Opens[i] = ModuleOpens.Read(stream);

        mod.UsesCount = TUShort.Read(stream);
        mod.UsesIndex = new TUShort[mod.UsesCount.Value];
        for (int i = 0; i < mod.UsesIndex.Length; i++) mod.UsesIndex[i] = TUShort.Read(stream);

        mod.ProvidesCount = TUShort.Read(stream);
        mod.Provides = new ModuleProvides[mod.ProvidesCount.Value];
        for (int i = 0; i < mod.Provides.Length; i++) mod.Provides[i] = ModuleProvides.Read(stream);

        return mod;
    }
}
