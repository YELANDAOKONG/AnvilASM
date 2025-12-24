using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes;

public class ModuleMainClassAttribute : IStructure<ModuleMainClassAttribute>, IAttribute
{
    public TUShort MainClassIndex { get; set; }

    public void Write(Stream stream) => MainClassIndex.Write(stream);

    public static ModuleMainClassAttribute Read(Stream stream) 
        => new() { MainClassIndex = TUShort.Read(stream) };
}