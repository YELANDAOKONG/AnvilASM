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
