using Anvil.Interfaces;

namespace Anvil.Structures.Attributes;

/// <summary>
/// Represents the Deprecated attribute (§4.7.15).
/// A fixed-length attribute with zero length.
/// </summary>
public class DeprecatedAttribute : IStructure<DeprecatedAttribute>, IAttribute
{
    public void Write(Stream stream)
    {
        // Empty body
    }

    public static DeprecatedAttribute Read(Stream stream)
    {
        return new DeprecatedAttribute();
    }
}