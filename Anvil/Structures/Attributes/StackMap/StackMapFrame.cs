using Anvil.Interfaces;
using Anvil.Structures.Attributes.StackMap.Frames;
using Anvil.Types;

namespace Anvil.Structures.Attributes.StackMap;

/// <summary>
/// Represents the stack_map_frame union (§4.7.4).
/// </summary>
public abstract class StackMapFrame : IStructure<StackMapFrame>
{
    public abstract byte FrameType { get; }

    public abstract void Write(Stream stream);

    public static StackMapFrame Read(Stream stream)
    {
        var frameType = TUByte.Read(stream).Value;

        if (frameType <= 63) 
            return new SameFrame(frameType);
        
        if (frameType <= 127) 
            return SameLocals1StackItemFrame.ReadBody(stream, frameType);
        
        if (frameType == 247) 
            return SameLocals1StackItemFrameExtended.ReadBody(stream);
        
        if (frameType >= 248 && frameType <= 250) 
            return ChopFrame.ReadBody(stream, frameType);
        
        if (frameType == 251) 
            return SameFrameExtended.ReadBody(stream);
        
        if (frameType >= 252 && frameType <= 254) 
            return AppendFrame.ReadBody(stream, frameType);
        
        if (frameType == 255) 
            return FullFrame.ReadBody(stream);

        throw new FormatException($"Unknown StackMapFrame type: {frameType}");
    }
}