using Anvil.Types;

namespace Anvil.Structures.Attributes.StackMap.Frames;

// §4.7.4 same_frame_extended
public class SameFrameExtended : StackMapFrame
{
    public override byte FrameType => 251;
    public TUShort OffsetDelta { get; set; }

    public SameFrameExtended(TUShort offsetDelta) => OffsetDelta = offsetDelta;

    public override void Write(Stream stream)
    {
        new TUByte(FrameType).Write(stream);
        OffsetDelta.Write(stream);
    }

    internal static SameFrameExtended ReadBody(Stream stream)
    {
        return new SameFrameExtended(TUShort.Read(stream));
    }
}