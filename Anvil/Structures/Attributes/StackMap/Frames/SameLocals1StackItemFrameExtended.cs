using Anvil.Types;

namespace Anvil.Structures.Attributes.StackMap.Frames;

// §4.7.4 same_locals_1_stack_item_frame_extended
public class SameLocals1StackItemFrameExtended : StackMapFrame
{
    public override byte FrameType => 247;
    public TUShort OffsetDelta { get; set; }
    public VerificationTypeInfo Stack { get; set; }

    public SameLocals1StackItemFrameExtended(TUShort offsetDelta, VerificationTypeInfo stack)
    {
        OffsetDelta = offsetDelta;
        Stack = stack;
    }

    public override void Write(Stream stream)
    {
        new TUByte(FrameType).Write(stream);
        OffsetDelta.Write(stream);
        Stack.Write(stream);
    }

    internal static SameLocals1StackItemFrameExtended ReadBody(Stream stream)
    {
        return new SameLocals1StackItemFrameExtended(TUShort.Read(stream), VerificationTypeInfo.Read(stream));
    }
}