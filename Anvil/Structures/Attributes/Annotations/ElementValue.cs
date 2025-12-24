using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes.Annotations;

/// <summary>
/// Represents the element_value structure (§4.7.16.1).
/// </summary>
public abstract class ElementValue : IStructure<ElementValue>
{
    public abstract byte Tag { get; }

    public virtual void Write(Stream stream)
    {
        new TUByte(Tag).Write(stream);
    }

    public static ElementValue Read(Stream stream)
    {
        var tag = TUByte.Read(stream).Value;
        return tag switch
        {
            (byte)'e' => EnumElementValue.ReadBody(stream),
            (byte)'c' => ClassElementValue.ReadBody(stream),
            (byte)'@' => AnnotationElementValue.ReadBody(stream),
            (byte)'[' => ArrayElementValue.ReadBody(stream),
            _ => ConstElementValue.ReadBody(stream, tag) // Primitives and String
        };
    }
}

public class ConstElementValue : ElementValue
{
    private readonly byte _tag;
    public override byte Tag => _tag;
    public TUShort ConstValueIndex { get; set; }

    public ConstElementValue(byte tag) => _tag = tag;

    public override void Write(Stream stream)
    {
        base.Write(stream);
        ConstValueIndex.Write(stream);
    }

    public static ConstElementValue ReadBody(Stream stream, byte tag)
    {
        return new ConstElementValue(tag) { ConstValueIndex = TUShort.Read(stream) };
    }
}

public class EnumElementValue : ElementValue
{
    public override byte Tag => (byte)'e';
    public TUShort TypeNameIndex { get; set; }
    public TUShort ConstNameIndex { get; set; }

    public override void Write(Stream stream)
    {
        base.Write(stream);
        TypeNameIndex.Write(stream);
        ConstNameIndex.Write(stream);
    }

    public static EnumElementValue ReadBody(Stream stream)
    {
        return new EnumElementValue
        {
            TypeNameIndex = TUShort.Read(stream),
            ConstNameIndex = TUShort.Read(stream)
        };
    }
}

public class ClassElementValue : ElementValue
{
    public override byte Tag => (byte)'c';
    public TUShort ClassInfoIndex { get; set; }

    public override void Write(Stream stream)
    {
        base.Write(stream);
        ClassInfoIndex.Write(stream);
    }

    public static ClassElementValue ReadBody(Stream stream)
    {
        return new ClassElementValue { ClassInfoIndex = TUShort.Read(stream) };
    }
}

public class AnnotationElementValue : ElementValue
{
    public override byte Tag => (byte)'@';
    public Annotation Annotation { get; set; }

    public override void Write(Stream stream)
    {
        base.Write(stream);
        Annotation.Write(stream);
    }

    public static AnnotationElementValue ReadBody(Stream stream)
    {
        return new AnnotationElementValue { Annotation = Annotation.Read(stream) };
    }
}

public class ArrayElementValue : ElementValue
{
    public override byte Tag => (byte)'[';
    public TUShort NumValues { get; set; }
    public ElementValue[] Values { get; set; } = Array.Empty<ElementValue>();

    public override void Write(Stream stream)
    {
        base.Write(stream);
        new TUShort((ushort)Values.Length).Write(stream);
        foreach (var val in Values) val.Write(stream);
    }

    public static ArrayElementValue ReadBody(Stream stream)
    {
        var val = new ArrayElementValue();
        val.NumValues = TUShort.Read(stream);
        val.Values = new ElementValue[val.NumValues.Value];
        for (int i = 0; i < val.Values.Length; i++)
        {
            val.Values[i] = ElementValue.Read(stream);
        }
        return val;
    }
}
