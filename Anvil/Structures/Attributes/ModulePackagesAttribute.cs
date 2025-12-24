using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes;

public class ModulePackagesAttribute : IStructure<ModulePackagesAttribute>, IAttribute
{
    public TUShort PackageCount { get; set; }
    public TUShort[] PackageIndex { get; set; } = Array.Empty<TUShort>();

    public void Write(Stream stream)
    {
        new TUShort((ushort)PackageIndex.Length).Write(stream);
        foreach (var idx in PackageIndex) idx.Write(stream);
    }

    public static ModulePackagesAttribute Read(Stream stream)
    {
        var attr = new ModulePackagesAttribute();
        attr.PackageCount = TUShort.Read(stream);
        attr.PackageIndex = new TUShort[attr.PackageCount.Value];
        for (int i = 0; i < attr.PackageIndex.Length; i++)
        {
            attr.PackageIndex[i] = TUShort.Read(stream);
        }
        return attr;
    }
}