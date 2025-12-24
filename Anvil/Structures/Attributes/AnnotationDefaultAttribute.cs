using Anvil.Interfaces;
using Anvil.Structures.Attributes.Annotations;

namespace Anvil.Structures.Attributes;

public class AnnotationDefaultAttribute : IStructure<AnnotationDefaultAttribute>, IAttribute
{
    public ElementValue DefaultValue { get; set; }

    public void Write(Stream stream)
    {
        DefaultValue.Write(stream);
    }

    public static AnnotationDefaultAttribute Read(Stream stream)
    {
        return new AnnotationDefaultAttribute
        {
            DefaultValue = ElementValue.Read(stream)
        };
    }
}