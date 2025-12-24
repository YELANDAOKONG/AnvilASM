using Anvil.Types;

namespace Anvil.Structures.Attributes.StackMap.Types;

public class UninitializedVariableInfo : VerificationTypeInfo
{
    public override byte Tag => 8;
    public TUShort Offset { get; set; }

    public UninitializedVariableInfo(TUShort offset) => Offset = offset;

    public override void Write(Stream stream)
    {
        base.Write(stream);
        Offset.Write(stream);
    }

    internal static UninitializedVariableInfo ReadBody(Stream stream)
    {
        return new UninitializedVariableInfo(TUShort.Read(stream));
    }
}