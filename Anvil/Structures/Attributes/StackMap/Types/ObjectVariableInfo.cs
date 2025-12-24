using Anvil.Types;

namespace Anvil.Structures.Attributes.StackMap.Types;

public class ObjectVariableInfo : VerificationTypeInfo
{
    public override byte Tag => 7;
    public TUShort CPoolIndex { get; set; }

    public ObjectVariableInfo(TUShort cPoolIndex) => CPoolIndex = cPoolIndex;

    public override void Write(Stream stream)
    {
        base.Write(stream);
        CPoolIndex.Write(stream);
    }

    internal static ObjectVariableInfo ReadBody(Stream stream)
    {
        return new ObjectVariableInfo(TUShort.Read(stream));
    }
}