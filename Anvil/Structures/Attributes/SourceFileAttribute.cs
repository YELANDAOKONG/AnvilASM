using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes;

public class SourceFileAttribute : IStructure<SourceFileAttribute>, IAttribute
{
    public TUShort SourceFileIndex { get; set; }

    public void Write(Stream stream) => SourceFileIndex.Write(stream);

    public static SourceFileAttribute Read(Stream stream) 
        => new() { SourceFileIndex = TUShort.Read(stream) };
}