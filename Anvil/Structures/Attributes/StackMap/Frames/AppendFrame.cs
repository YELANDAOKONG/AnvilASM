using Anvil.Types;

namespace Anvil.Structures.Attributes.StackMap.Frames;

// §4.7.4 append_frame
public class AppendFrame : StackMapFrame
{
    private readonly byte _frameType;
    public override byte FrameType => _frameType;
    public TUShort OffsetDelta { get; set; }
    public VerificationTypeInfo[] Locals { get; set; }

    public AppendFrame(byte frameType, TUShort offsetDelta, VerificationTypeInfo[] locals)
    {
        _frameType = frameType;
        OffsetDelta = offsetDelta;
        Locals = locals;
    }

    public override void Write(Stream stream)
    {
        new TUByte(FrameType).Write(stream);
        OffsetDelta.Write(stream);
        foreach (var local in Locals) local.Write(stream);
    }

    internal static AppendFrame ReadBody(Stream stream, byte frameType)
    {
        var offsetDelta = TUShort.Read(stream);
        int k = frameType - 251;
        var locals = new VerificationTypeInfo[k];
        for (int i = 0; i < k; i++)
        {
            locals[i] = VerificationTypeInfo.Read(stream);
        }
        return new AppendFrame(frameType, offsetDelta, locals);
    }
}