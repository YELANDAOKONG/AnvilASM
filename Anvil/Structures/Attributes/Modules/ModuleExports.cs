using Anvil.Constants.Flags;
using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes.Modules;

public class ModuleExports : IStructure<ModuleExports>
{
    public TUShort ExportsIndex { get; set; }
    public ModuleExportsFlags ExportsFlags { get; set; }
    public TUShort ExportsToCount { get; set; }
    public TUShort[] ExportsToIndex { get; set; } = Array.Empty<TUShort>();

    public void Write(Stream stream)
    {
        ExportsIndex.Write(stream);
        new TUShort((ushort)ExportsFlags).Write(stream);
        new TUShort((ushort)ExportsToIndex.Length).Write(stream);
        foreach (var idx in ExportsToIndex) idx.Write(stream);
    }

    public static ModuleExports Read(Stream stream)
    {
        var exp = new ModuleExports();
        exp.ExportsIndex = TUShort.Read(stream);
        exp.ExportsFlags = (ModuleExportsFlags)TUShort.Read(stream).Value;
        exp.ExportsToCount = TUShort.Read(stream);
        exp.ExportsToIndex = new TUShort[exp.ExportsToCount.Value];
        for (int i = 0; i < exp.ExportsToIndex.Length; i++) exp.ExportsToIndex[i] = TUShort.Read(stream);
        return exp;
    }
}