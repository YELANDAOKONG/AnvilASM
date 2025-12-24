using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes.TypeAnnotations;

public class TypePath : IStructure<TypePath>
{
    public TUByte PathLength { get; set; }
    public TypePathEntry[] Path { get; set; } = Array.Empty<TypePathEntry>();

    public void Write(Stream stream)
    {
        new TUByte((byte)Path.Length).Write(stream);
        foreach (var entry in Path) entry.Write(stream);
    }

    public static TypePath Read(Stream stream)
    {
        var tp = new TypePath();
        tp.PathLength = TUByte.Read(stream);
        tp.Path = new TypePathEntry[tp.PathLength.Value];
        for (int i = 0; i < tp.Path.Length; i++) tp.Path[i] = TypePathEntry.Read(stream);
        return tp;
    }
}

public class TypePathEntry : IStructure<TypePathEntry>
{
    public TUByte TypePathKind { get; set; }
    public TUByte TypeArgumentIndex { get; set; }

    public void Write(Stream stream) { TypePathKind.Write(stream); TypeArgumentIndex.Write(stream); }
    public static TypePathEntry Read(Stream stream) => new() { TypePathKind = TUByte.Read(stream), TypeArgumentIndex = TUByte.Read(stream) };
}