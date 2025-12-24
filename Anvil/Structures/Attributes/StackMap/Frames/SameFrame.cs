using Anvil.Types;

namespace Anvil.Structures.Attributes.StackMap.Frames;

// §4.7.4 same_frame
public class SameFrame : StackMapFrame
{
    private readonly byte _frameType;
    public override byte FrameType => _frameType;

    public SameFrame(byte frameType) => _frameType = frameType;

    public override void Write(Stream stream) => new TUByte(FrameType).Write(stream);
}