using Anvil.Interfaces;
using Anvil.Structures.Attributes;

namespace Anvil.Factories;

public static class AttributeFactory
{
    /// <summary>
    /// Parses the raw bytes of an attribute into a specific IAttribute implementation.
    /// </summary>
    /// <param name="name">The resolved name of the attribute (e.g., "Code", "InnerClasses").</param>
    /// <param name="data">The raw info[] bytes.</param>
    /// <returns>A specific implementation of IAttribute, or null if unknown/unsupported.</returns>
    public static IAttribute? Create(string name, byte[] data)
    {
        using var stream = new MemoryStream(data);

        return name switch
        {
            "InnerClasses" => InnerClassesAttribute.Read(stream),
            "SourceFile"   => SourceFileAttribute.Read(stream),
            // "Code"      => CodeAttribute.Read(stream),
            // "Signature" => SignatureAttribute.Read(stream),
            _              => null
        };
    }
}