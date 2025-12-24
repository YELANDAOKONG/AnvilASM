namespace Anvil.Interfaces;

/// <summary>
/// Defines a contract for low-level JVM structures (composed of multiple IType primitives).
/// Responsible for serializing and deserializing composite binary structures.
/// </summary>
/// <typeparam name="TSelf">The type of the structure implementation.</typeparam>
public interface IStructure<TSelf> where TSelf : IStructure<TSelf>
{
    /// <summary>
    /// Writes the complete structure to the stream.
    /// </summary>
    void Write(Stream stream);

    /// <summary>
    /// Reads the complete structure from the stream.
    /// </summary>
    static abstract TSelf Read(Stream stream);
    
    byte[] ToBytes()
    {
        using var stream = new MemoryStream();
        Write(stream);
        return stream.ToArray();
    }
}