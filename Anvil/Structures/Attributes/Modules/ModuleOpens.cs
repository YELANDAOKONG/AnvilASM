using Anvil.Constants.Flags;
using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes.Modules;

public class ModuleOpens : IStructure<ModuleOpens>
{
    public TUShort OpensIndex { get; set; }
    public ModuleOpensFlags OpensFlags { get; set; }
    public TUShort OpensToCount { get; set; }
    public TUShort[] OpensToIndex { get; set; } = Array.Empty<TUShort>();

    public void Write(Stream stream)
    {
        OpensIndex.Write(stream);
        new TUShort((ushort)OpensFlags).Write(stream);
        new TUShort((ushort)OpensToIndex.Length).Write(stream);
        foreach (var idx in OpensToIndex) idx.Write(stream);
    }

    public static ModuleOpens Read(Stream stream)
    {
        var opn = new ModuleOpens();
        opn.OpensIndex = TUShort.Read(stream);
        opn.OpensFlags = (ModuleOpensFlags)TUShort.Read(stream).Value;
        opn.OpensToCount = TUShort.Read(stream);
        opn.OpensToIndex = new TUShort[opn.OpensToCount.Value];
        for (int i = 0; i < opn.OpensToIndex.Length; i++) opn.OpensToIndex[i] = TUShort.Read(stream);
        return opn;
    }
}