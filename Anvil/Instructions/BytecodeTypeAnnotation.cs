using Anvil.Structures.Attributes.TypeAnnotations;

namespace Anvil.Instructions;

public class BytecodeTypeAnnotation
{
    public bool IsVisible { get; set; }
    public TypeAnnotation Annotation { get; set; }
    public Label? OffsetTarget { get; set; }
    public List<(Label Start, Label End, int Index)> LocalVariableTargets { get; } = [];
    public TryCatchBlock? CatchTarget { get; set; }

    public BytecodeTypeAnnotation(bool isVisible, TypeAnnotation annotation)
    {
        IsVisible = isVisible;
        Annotation = annotation ?? throw new ArgumentNullException(nameof(annotation));
    }
}
