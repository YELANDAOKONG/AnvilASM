namespace Anvil.Interfaces;

public interface IType<TSelf> where TSelf : IType<TSelf>
{
    /// <summary>
    /// Writes the type to the provided stream (Big-Endian).
    /// </summary>
    void Write(Stream stream);

    /// <summary>
    /// Reads the type from the provided stream (Big-Endian).
    /// </summary>
    static abstract TSelf Read(Stream stream);
    
    byte[] ToBytes()
    {
        using var stream = new MemoryStream();
        Write(stream);
        return stream.ToArray();
    }
}