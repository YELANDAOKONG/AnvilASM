using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes;

/// <summary>
/// Represents the Signature attribute (§4.7.9).
/// </summary>
public class SignatureAttribute : IStructure<SignatureAttribute>, IAttribute
{
    public TUShort SignatureIndex { get; set; }

    public void Write(Stream stream)
    {
        SignatureIndex.Write(stream);
    }

    public static SignatureAttribute Read(Stream stream)
    {
        return new SignatureAttribute
        {
            SignatureIndex = TUShort.Read(stream)
        };
    }
}