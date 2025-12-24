using Anvil.Interfaces;

namespace Anvil.Structures.Attributes;

/// <summary>
/// Represents the SourceDebugExtension attribute (§4.7.11).
/// </summary>
public class SourceDebugExtensionAttribute : IAttribute
{
    /// <summary>
    /// Holds extended debugging information. 
    /// Represented using modified UTF-8 with no terminating zero byte.
    /// </summary>
    public byte[] DebugExtension { get; set; }

    public SourceDebugExtensionAttribute(byte[] debugExtension)
    {
        DebugExtension = debugExtension;
    }

    public void Write(Stream stream)
    {
        stream.Write(DebugExtension);
    }

    /// <summary>
    /// Note: This attribute is variable length. The length is determined by the 
    /// attribute_length field in the parent AttributeInfo, so the factory 
    /// passes the full byte array here.
    /// </summary>
    public static SourceDebugExtensionAttribute Read(byte[] data)
    {
        return new SourceDebugExtensionAttribute(data);
    }
}