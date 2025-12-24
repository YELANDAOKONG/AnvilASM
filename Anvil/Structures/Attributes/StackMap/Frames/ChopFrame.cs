using Anvil.Types;

namespace Anvil.Structures.Attributes.StackMap.Frames;

// §4.7.4 chop_frame
public class ChopFrame : StackMapFrame
{
    private readonly byte _frameType;
    public override byte FrameType => _frameType;
    public TUShort OffsetDelta { get; set; }

    public ChopFrame(byte frameType, TUShort offsetDelta)
    {
        _frameType = frameType;
        OffsetDelta = offsetDelta;
    }

    public override void Write(Stream stream)
    {
        new TUByte(FrameType).Write(stream);
        OffsetDelta.Write(stream);
    }

    internal static ChopFrame ReadBody(Stream stream, byte frameType)
    {
        return new ChopFrame(frameType, TUShort.Read(stream));
    }
}