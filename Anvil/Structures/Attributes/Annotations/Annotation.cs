using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes.Annotations;

public class Annotation : IStructure<Annotation>
{
    public TUShort TypeIndex { get; set; }
    public TUShort NumElementValuePairs { get; set; }
    public ElementValuePair[] ElementValuePairs { get; set; } = Array.Empty<ElementValuePair>();

    public void Write(Stream stream)
    {
        TypeIndex.Write(stream);
        new TUShort((ushort)ElementValuePairs.Length).Write(stream);
        foreach (var pair in ElementValuePairs) pair.Write(stream);
    }

    public static Annotation Read(Stream stream)
    {
        var ann = new Annotation();
        ann.TypeIndex = TUShort.Read(stream);
        ann.NumElementValuePairs = TUShort.Read(stream);
        ann.ElementValuePairs = new ElementValuePair[ann.NumElementValuePairs.Value];
        for (int i = 0; i < ann.ElementValuePairs.Length; i++)
        {
            ann.ElementValuePairs[i] = ElementValuePair.Read(stream);
        }
        return ann;
    }
}

public class ElementValuePair : IStructure<ElementValuePair>
{
    public TUShort ElementNameIndex { get; set; }
    public ElementValue Value { get; set; }

    public void Write(Stream stream)
    {
        ElementNameIndex.Write(stream);
        Value.Write(stream);
    }

    public static ElementValuePair Read(Stream stream)
    {
        return new ElementValuePair
        {
            ElementNameIndex = TUShort.Read(stream),
            Value = ElementValue.Read(stream)
        };
    }
}