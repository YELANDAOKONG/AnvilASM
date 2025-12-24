using Anvil.Types;

namespace Anvil.Structures.Attributes.StackMap.Frames;

// §4.7.4 same_locals_1_stack_item_frame
public class SameLocals1StackItemFrame : StackMapFrame
{
    private readonly byte _frameType;
    public override byte FrameType => _frameType;
    public VerificationTypeInfo Stack { get; set; }

    public SameLocals1StackItemFrame(byte frameType, VerificationTypeInfo stack)
    {
        _frameType = frameType;
        Stack = stack;
    }

    public override void Write(Stream stream)
    {
        new TUByte(FrameType).Write(stream);
        Stack.Write(stream);
    }

    internal static SameLocals1StackItemFrame ReadBody(Stream stream, byte frameType)
    {
        return new SameLocals1StackItemFrame(frameType, VerificationTypeInfo.Read(stream));
    }
}