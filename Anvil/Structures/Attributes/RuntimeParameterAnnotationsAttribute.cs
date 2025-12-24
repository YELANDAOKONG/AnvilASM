using Anvil.Interfaces;
using Anvil.Structures.Attributes.Annotations;
using Anvil.Types;

namespace Anvil.Structures.Attributes;

public abstract class RuntimeParameterAnnotationsAttribute : IStructure<RuntimeParameterAnnotationsAttribute>, IAttribute
{
    public TUByte NumParameters { get; set; }
    public ParameterAnnotationEntry[] ParameterAnnotations { get; set; } = Array.Empty<ParameterAnnotationEntry>();

    public void Write(Stream stream)
    {
        new TUByte((byte)ParameterAnnotations.Length).Write(stream);
        foreach (var entry in ParameterAnnotations) entry.Write(stream);
    }

    public static RuntimeParameterAnnotationsAttribute Read(Stream stream, RuntimeParameterAnnotationsAttribute instance)
    {
        instance.NumParameters = TUByte.Read(stream);
        instance.ParameterAnnotations = new ParameterAnnotationEntry[instance.NumParameters.Value];
        for (int i = 0; i < instance.ParameterAnnotations.Length; i++)
        {
            instance.ParameterAnnotations[i] = ParameterAnnotationEntry.Read(stream);
        }
        return instance;
    }

    public static RuntimeParameterAnnotationsAttribute Read(Stream stream) => throw new NotImplementedException();
}

public class ParameterAnnotationEntry : IStructure<ParameterAnnotationEntry>
{
    public TUShort NumAnnotations { get; set; }
    public Annotation[] Annotations { get; set; } = Array.Empty<Annotation>();

    public void Write(Stream stream)
    {
        new TUShort((ushort)Annotations.Length).Write(stream);
        foreach (var ann in Annotations) ann.Write(stream);
    }

    public static ParameterAnnotationEntry Read(Stream stream)
    {
        var entry = new ParameterAnnotationEntry();
        entry.NumAnnotations = TUShort.Read(stream);
        entry.Annotations = new Annotation[entry.NumAnnotations.Value];
        for (int i = 0; i < entry.Annotations.Length; i++)
        {
            entry.Annotations[i] = Annotation.Read(stream);
        }
        return entry;
    }
}

public class RuntimeVisibleParameterAnnotationsAttribute : RuntimeParameterAnnotationsAttribute
{
    public new static RuntimeVisibleParameterAnnotationsAttribute Read(Stream stream) 
        => (RuntimeVisibleParameterAnnotationsAttribute)Read(stream, new RuntimeVisibleParameterAnnotationsAttribute());
}

public class RuntimeInvisibleParameterAnnotationsAttribute : RuntimeParameterAnnotationsAttribute
{
    public new static RuntimeInvisibleParameterAnnotationsAttribute Read(Stream stream) 
        => (RuntimeInvisibleParameterAnnotationsAttribute)Read(stream, new RuntimeInvisibleParameterAnnotationsAttribute());
}
