using Anvil.Interfaces;
using Anvil.Structures.Attributes.TypeAnnotations;
using Anvil.Types;

namespace Anvil.Structures.Attributes;

public abstract class RuntimeTypeAnnotationsAttribute : IStructure<RuntimeTypeAnnotationsAttribute>, IAttribute
{
    public TUShort NumAnnotations { get; set; }
    public TypeAnnotation[] Annotations { get; set; } = Array.Empty<TypeAnnotation>();

    public void Write(Stream stream)
    {
        new TUShort((ushort)Annotations.Length).Write(stream);
        foreach (var ann in Annotations) ann.Write(stream);
    }

    public static RuntimeTypeAnnotationsAttribute Read(Stream stream, RuntimeTypeAnnotationsAttribute instance)
    {
        instance.NumAnnotations = TUShort.Read(stream);
        instance.Annotations = new TypeAnnotation[instance.NumAnnotations.Value];
        for (int i = 0; i < instance.Annotations.Length; i++)
        {
            instance.Annotations[i] = TypeAnnotation.Read(stream);
        }
        return instance;
    }
    
    public static RuntimeTypeAnnotationsAttribute Read(Stream stream) => throw new NotImplementedException();
}

public class RuntimeVisibleTypeAnnotationsAttribute : RuntimeTypeAnnotationsAttribute
{
    public new static RuntimeVisibleTypeAnnotationsAttribute Read(Stream stream) 
        => (RuntimeVisibleTypeAnnotationsAttribute)Read(stream, new RuntimeVisibleTypeAnnotationsAttribute());
}

public class RuntimeInvisibleTypeAnnotationsAttribute : RuntimeTypeAnnotationsAttribute
{
    public new static RuntimeInvisibleTypeAnnotationsAttribute Read(Stream stream) 
        => (RuntimeInvisibleTypeAnnotationsAttribute)Read(stream, new RuntimeInvisibleTypeAnnotationsAttribute());
}