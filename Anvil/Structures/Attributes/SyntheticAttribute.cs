using Anvil.Interfaces;

namespace Anvil.Structures.Attributes;

/// <summary>
/// Represents the Synthetic attribute (§4.7.8).
/// A fixed-length attribute with zero length.
/// </summary>
public class SyntheticAttribute : IStructure<SyntheticAttribute>, IAttribute
{
    public void Write(Stream stream)
    {
        // Empty body
    }

    public static SyntheticAttribute Read(Stream stream)
    {
        return new SyntheticAttribute();
    }
}