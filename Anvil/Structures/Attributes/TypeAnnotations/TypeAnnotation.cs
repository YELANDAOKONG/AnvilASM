using Anvil.Interfaces;
using Anvil.Structures.Attributes.Annotations;
using Anvil.Types;

namespace Anvil.Structures.Attributes.TypeAnnotations;

public class TypeAnnotation : IStructure<TypeAnnotation>
{
    public TUByte TargetType { get; set; }
    public TargetInfo TargetInfo { get; set; }
    public TypePath TargetPath { get; set; }
    
    // Fields from standard Annotation
    public TUShort TypeIndex { get; set; }
    public TUShort NumElementValuePairs { get; set; }
    public ElementValuePair[] ElementValuePairs { get; set; } = Array.Empty<ElementValuePair>();

    public void Write(Stream stream)
    {
        TargetType.Write(stream);
        TargetInfo.Write(stream);
        TargetPath.Write(stream);
        
        TypeIndex.Write(stream);
        new TUShort((ushort)ElementValuePairs.Length).Write(stream);
        foreach (var pair in ElementValuePairs) pair.Write(stream);
    }

    public static TypeAnnotation Read(Stream stream)
    {
        var ta = new TypeAnnotation();
        ta.TargetType = TUByte.Read(stream);
        ta.TargetInfo = TargetInfo.Read(stream, ta.TargetType.Value);
        ta.TargetPath = TypePath.Read(stream);
        
        ta.TypeIndex = TUShort.Read(stream);
        ta.NumElementValuePairs = TUShort.Read(stream);
        ta.ElementValuePairs = new ElementValuePair[ta.NumElementValuePairs.Value];
        for (int i = 0; i < ta.ElementValuePairs.Length; i++)
        {
            ta.ElementValuePairs[i] = ElementValuePair.Read(stream);
        }
        return ta;
    }
}