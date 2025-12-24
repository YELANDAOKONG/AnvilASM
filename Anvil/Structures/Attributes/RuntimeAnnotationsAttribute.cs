using Anvil.Interfaces;
using Anvil.Structures.Attributes.Annotations;
using Anvil.Types;

namespace Anvil.Structures.Attributes;

public abstract class RuntimeAnnotationsAttribute : IStructure<RuntimeAnnotationsAttribute>, IAttribute
{
    public TUShort NumAnnotations { get; set; }
    public Annotation[] Annotations { get; set; } = Array.Empty<Annotation>();

    public void Write(Stream stream)
    {
        new TUShort((ushort)Annotations.Length).Write(stream);
        foreach (var ann in Annotations) ann.Write(stream);
    }

    public static RuntimeAnnotationsAttribute Read(Stream stream, RuntimeAnnotationsAttribute instance)
    {
        instance.NumAnnotations = TUShort.Read(stream);
        instance.Annotations = new Annotation[instance.NumAnnotations.Value];
        for (int i = 0; i < instance.Annotations.Length; i++)
        {
            instance.Annotations[i] = Annotation.Read(stream);
        }
        return instance;
    }
    
    // Interface compliance
    public static RuntimeAnnotationsAttribute Read(Stream stream) => throw new NotImplementedException("Use specific subclass Read methods.");
}

public class RuntimeVisibleAnnotationsAttribute : RuntimeAnnotationsAttribute
{
    public new static RuntimeVisibleAnnotationsAttribute Read(Stream stream) 
        => (RuntimeVisibleAnnotationsAttribute)Read(stream, new RuntimeVisibleAnnotationsAttribute());
}

public class RuntimeInvisibleAnnotationsAttribute : RuntimeAnnotationsAttribute
{
    public new static RuntimeInvisibleAnnotationsAttribute Read(Stream stream) 
        => (RuntimeInvisibleAnnotationsAttribute)Read(stream, new RuntimeInvisibleAnnotationsAttribute());
}