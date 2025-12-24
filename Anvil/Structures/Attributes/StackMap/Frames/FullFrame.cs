using Anvil.Types;

namespace Anvil.Structures.Attributes.StackMap.Frames;

// §4.7.4 full_frame
public class FullFrame : StackMapFrame
{
    public override byte FrameType => 255;
    public TUShort OffsetDelta { get; set; }
    public VerificationTypeInfo[] Locals { get; set; }
    public VerificationTypeInfo[] Stack { get; set; }

    public FullFrame(TUShort offsetDelta, VerificationTypeInfo[] locals, VerificationTypeInfo[] stack)
    {
        OffsetDelta = offsetDelta;
        Locals = locals;
        Stack = stack;
    }

    public override void Write(Stream stream)
    {
        new TUByte(FrameType).Write(stream);
        OffsetDelta.Write(stream);
        
        new TUShort((ushort)Locals.Length).Write(stream);
        foreach (var local in Locals) local.Write(stream);
        
        new TUShort((ushort)Stack.Length).Write(stream);
        foreach (var item in Stack) item.Write(stream);
    }

    internal static FullFrame ReadBody(Stream stream)
    {
        var offsetDelta = TUShort.Read(stream);
        
        var numberOfLocals = TUShort.Read(stream).Value;
        var locals = new VerificationTypeInfo[numberOfLocals];
        for (int i = 0; i < numberOfLocals; i++)
        {
            locals[i] = VerificationTypeInfo.Read(stream);
        }

        var numberOfStackItems = TUShort.Read(stream).Value;
        var stack = new VerificationTypeInfo[numberOfStackItems];
        for (int i = 0; i < numberOfStackItems; i++)
        {
            stack[i] = VerificationTypeInfo.Read(stream);
        }

        return new FullFrame(offsetDelta, locals, stack);
    }
}