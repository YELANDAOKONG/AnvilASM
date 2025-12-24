using Anvil.Constants.Flags;
using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes.Modules;

public class ModuleRequires : IStructure<ModuleRequires>
{
    public TUShort RequiresIndex { get; set; }
    public ModuleRequiresFlags RequiresFlags { get; set; }
    public TUShort RequiresVersionIndex { get; set; }

    public void Write(Stream stream)
    {
        RequiresIndex.Write(stream);
        new TUShort((ushort)RequiresFlags).Write(stream);
        RequiresVersionIndex.Write(stream);
    }

    public static ModuleRequires Read(Stream stream) => new()
    {
        RequiresIndex = TUShort.Read(stream),
        RequiresFlags = (ModuleRequiresFlags)TUShort.Read(stream).Value,
        RequiresVersionIndex = TUShort.Read(stream)
    };
}