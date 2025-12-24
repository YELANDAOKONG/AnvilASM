using Anvil.Instructions;

namespace Anvil.Serialization;

/// <summary>
/// Handles the serialization of high-level Code objects back to raw bytecode.
/// Ensures correct WIDE prefix injection and Switch alignment padding.
/// </summary>
public class BytecodeWriter
{
    private readonly MemoryStream _stream;

    public BytecodeWriter()
    {
        _stream = new MemoryStream();
    }

    public static byte[] Write(IEnumerable<Code> instructions)
    {
        var writer = new BytecodeWriter();
        foreach (var code in instructions)
        {
            writer.WriteInstruction(code);
        }
        return writer._stream.ToArray();
    }

    public void WriteInstruction(Code code)
    {
        // 1. Write WIDE prefix if the Code object flagged it
        if (code.WidePrefix.HasValue)
        {
            _stream.WriteByte((byte)code.WidePrefix.Value);
        }

        // 2. Write OpCode
        _stream.WriteByte((byte)code.OpCode);

        // 3. Handle Variable-Length Instructions (Switches)
        // These require dynamic padding based on the current stream position
        if (code.OpCode == OperationCode.TABLESWITCH || code.OpCode == OperationCode.LOOKUPSWITCH)
        {
            WriteSwitchOperands(code);
            return;
        }

        // 4. Write Standard Operands
        // The Operand.Data array already contains the correctly formatted bytes (BigEndian)
        foreach (var operand in code.Operands)
        {
            _stream.Write(operand.Data);
        }
    }

    private void WriteSwitchOperands(Code code)
    {
        // Spec §4.7.3:
        // Immediately after the opcode, between 0 and 3 bytes must be added as padding
        // so that the defaultbyte1 begins at an address that is a multiple of 4 bytes
        // from the start of the current method.
        
        long currentPos = _stream.Position;
        
        // Calculate padding (0-3 bytes)
        int padding = (int)((4 - (currentPos % 4)) % 4);

        for (int i = 0; i < padding; i++)
        {
            _stream.WriteByte(0);
        }

        // Write the actual switch data (Default, Pairs/Offsets)
        // Note: The Operand.Data for switches (created via factory) contains the 
        // structured integers (default, low, high, offsets...), but NOT the padding.
        if (code.Operands.Count > 0)
        {
            _stream.Write(code.Operands[0].Data);
        }
        else
        {
            // Should not happen if created via Factory, but safe fallback
            throw new InvalidOperationException("Switch instruction missing operand data.");
        }
    }
}
