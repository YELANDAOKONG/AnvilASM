using Anvil.Interfaces;
using Anvil.Structures.Attributes.StackMap.Types;
using Anvil.Types;

namespace Anvil.Structures.Attributes.StackMap;

/// <summary>
/// Represents the verification_type_info union (§4.7.4).
/// </summary>
public abstract class VerificationTypeInfo : IStructure<VerificationTypeInfo>
{
    public abstract byte Tag { get; }

    public virtual void Write(Stream stream)
    {
        new TUByte(Tag).Write(stream);
    }

    public static VerificationTypeInfo Read(Stream stream)
    {
        var tag = TUByte.Read(stream).Value;
        return tag switch
        {
            0 => new TopVariableInfo(),
            1 => new IntegerVariableInfo(),
            2 => new FloatVariableInfo(),
            3 => new DoubleVariableInfo(),
            4 => new LongVariableInfo(),
            5 => new NullVariableInfo(),
            6 => new UninitializedThisVariableInfo(),
            7 => ObjectVariableInfo.ReadBody(stream),
            8 => UninitializedVariableInfo.ReadBody(stream),
            _ => throw new FormatException($"Unknown VerificationTypeInfo tag: {tag}")
        };
    }
}