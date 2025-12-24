using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes;

/// <summary>
/// Represents the EnclosingMethod attribute (§4.7.7).
/// </summary>
public class EnclosingMethodAttribute : IStructure<EnclosingMethodAttribute>, IAttribute
{
    public TUShort ClassIndex { get; set; }
    public TUShort MethodIndex { get; set; }

    public void Write(Stream stream)
    {
        ClassIndex.Write(stream);
        MethodIndex.Write(stream);
    }

    public static EnclosingMethodAttribute Read(Stream stream)
    {
        return new EnclosingMethodAttribute
        {
            ClassIndex = TUShort.Read(stream),
            MethodIndex = TUShort.Read(stream)
        };
    }
}