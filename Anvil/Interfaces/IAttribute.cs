namespace Anvil.Interfaces;

/// <summary>
/// Represents the parsed body (info[]) of an attribute.
/// </summary>
public interface IAttribute
{
    void Write(Stream stream);
}