using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes;

/// <summary>
/// Represents the ConstantValue attribute (§4.7.2).
/// </summary>
public class ConstantValueAttribute : IAttribute
{
    public TUShort ConstantValueIndex { get; set; }

    public ConstantValueAttribute(TUShort constantValueIndex)
    {
        ConstantValueIndex = constantValueIndex;
    }

    public void Write(Stream stream)
    {
        ConstantValueIndex.Write(stream);
    }

    public static ConstantValueAttribute Read(Stream stream)
    {
        return new ConstantValueAttribute(TUShort.Read(stream));
    }
}