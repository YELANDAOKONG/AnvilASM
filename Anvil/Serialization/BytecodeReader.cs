using System.Buffers.Binary;
using Anvil.Instructions;

namespace Anvil.Serialization;

/// <summary>
/// A strict JVM bytecode parser.
/// Handles WIDE expansion, Switch alignment padding, and opcode-specific operand parsing.
/// </summary>
public class BytecodeReader
{
    private readonly byte[] _bytes;
    private int _pos;

    public BytecodeReader(byte[] bytes)
    {
        _bytes = bytes;
        _pos = 0;
    }

    /// <summary>
    /// Parses a raw byte array into a list of high-level Code instructions.
    /// </summary>
    public static List<Code> Read(byte[] code)
    {
        var reader = new BytecodeReader(code);
        return reader.ReadAll();
    }

    public List<Code> ReadAll()
    {
        var instructions = new List<Code>();
        _pos = 0;

        while (_pos < _bytes.Length)
        {
            instructions.Add(ReadNext());
        }

        return instructions;
    }

    private Code ReadNext()
    {
        // 1. Read OpCode
        byte opByte = ReadU1();
        var opCode = (OperationCode)opByte;

        // 2. Handle WIDE prefix (0xC4)
        // Spec §4.7.3: The wide instruction modifies the behavior of the immediate next instruction.
        if (opCode == OperationCode.WIDE)
        {
            byte modifiedOpByte = ReadU1();
            var modifiedOpCode = (OperationCode)modifiedOpByte;
            return ReadWideInstruction(modifiedOpCode);
        }

        // 3. Handle Variable-Length Instructions (Switches)
        if (opCode == OperationCode.TABLESWITCH) return ReadTableSwitch();
        if (opCode == OperationCode.LOOKUPSWITCH) return ReadLookupSwitch();

        // 4. Handle Standard Instructions
        return ReadStandardInstruction(opCode);
    }

    private Code ReadStandardInstruction(OperationCode opCode)
    {
        if (!OperationCodeMapping.TryGetInfo(opCode, out var info))
            throw new InvalidOperationException($"Unknown or unsupported OpCode: {opCode} at offset {_pos - 1}");

        var operands = new List<Operand>();

        // Special handling for instructions with specific byte layouts
        if (opCode == OperationCode.INVOKEINTERFACE)
        {
            // Format: indexbyte1, indexbyte2, count, 0
            ushort index = ReadU2();
            byte count = ReadU1();
            byte zero = ReadU1(); // Must be 0
            if (zero != 0) 
                throw new FormatException($"INVOKEINTERFACE 4th operand must be 0, found {zero}");
            
            operands.Add(Operand.InvokeInterface(index, count));
        }
        else if (opCode == OperationCode.INVOKEDYNAMIC)
        {
            ushort index = ReadU2();
            ReadU2(); // Validate and discard 0's from the original stream, but regenerate when building the object
            operands.Add(Operand.InvokeDynamic(index));
        }

        else if (opCode == OperationCode.MULTIANEWARRAY)
        {
            // Format: indexbyte1, indexbyte2, dimensions
            ushort index = ReadU2();
            byte dims = ReadU1();
            operands.Add(Operand.MultiANewArray(index, dims));
        }
        else if (opCode == OperationCode.IINC)
        {
            // Format: index (u1), const (s1)
            byte index = ReadU1();
            sbyte constVal = ReadS1();
            operands.Add(Operand.Iinc(index, constVal));
        }
        else
        {
            // Generic handling based on operand definition
            foreach (var operandDef in info!.Operands)
            {
                operands.Add(ReadGenericOperand(opCode, operandDef.Size, operandDef.Type));
            }
        }

        return new Code(opCode, operands.ToArray());
    }

    private Code ReadWideInstruction(OperationCode opCode)
    {
        // Wide format 1: wide <opcode> <indexbyte1> <indexbyte2>
        // Wide format 2: wide iinc <indexbyte1> <indexbyte2> <constbyte1> <constbyte2>

        if (opCode == OperationCode.IINC)
        {
            ushort index = ReadU2();
            short constant = ReadS2();
            return Code.IInc(index, constant);
        }

        // For loads, stores, and ret, the index becomes 2 bytes
        ushort wideIndex = ReadU2();

        return opCode switch
        {
            OperationCode.ILOAD => Code.ILoad(wideIndex),
            OperationCode.LLOAD => Code.LLoad(wideIndex),
            OperationCode.FLOAD => Code.FLoad(wideIndex),
            OperationCode.DLOAD => Code.DLoad(wideIndex),
            OperationCode.ALOAD => Code.ALoad(wideIndex),
            OperationCode.ISTORE => Code.IStore(wideIndex),
            OperationCode.LSTORE => Code.LStore(wideIndex),
            OperationCode.FSTORE => Code.FStore(wideIndex),
            OperationCode.DSTORE => Code.DStore(wideIndex),
            OperationCode.ASTORE => Code.AStore(wideIndex),
            OperationCode.RET => new Code(OperationCode.RET, Operand.WideLocalIndex(wideIndex)),
            _ => throw new FormatException($"OpCode {opCode} is not valid for WIDE modification.")
        };
    }

    private Code ReadTableSwitch()
    {
        // Alignment padding
        int padding = (4 - (_pos % 4)) % 4;
        _pos += padding;

        int defaultOffset = ReadS4();
        int low = ReadS4();
        int high = ReadS4();

        // Use long to prevent overflow if high is large positive and low is large negative
        long count = (long)high - low + 1;
        if (count < 0 || count > 100000)
            throw new FormatException($"Invalid TABLESWITCH range: low={low}, high={high}, count={count}");

        int[] offsets = new int[count];
        for (int i = 0; i < count; i++)
        {
            offsets[i] = ReadS4();
        }

        return Code.TableSwitch(defaultOffset, low, high, offsets);
    }

    private Code ReadLookupSwitch()
    {
        // Alignment padding
        int padding = (4 - (_pos % 4)) % 4;
        _pos += padding;

        int defaultOffset = ReadS4();
        int npairs = ReadS4();

        if (npairs < 0 || npairs > 100000) 
            throw new FormatException($"Invalid LOOKUPSWITCH pairs count: {npairs}");

        var pairs = new (int match, int offset)[npairs];
        for (int i = 0; i < npairs; i++)
        {
            int match = ReadS4();
            int offset = ReadS4();
            pairs[i] = (match, offset);
        }

        return Code.LookupSwitch(defaultOffset, pairs);
    }

    /// <summary>
    /// Reads a generic operand based on size and semantic type.
    /// </summary>
    private Operand ReadGenericOperand(OperationCode context, int size, OperandType semanticType)
    {
        // If the mapping provided a type hint, use it
        if (semanticType != OperandType.None)
        {
            return semanticType switch
            {
                OperandType.LocalIndex => size == 1 ? Operand.LocalIndex(ReadU1()) : Operand.WideLocalIndex(ReadU2()),
                OperandType.ConstantPoolIndex => size == 1 ? Operand.ConstantPoolIndex(ReadU1()) : Operand.ConstantPoolIndex(ReadU2()),
                OperandType.ByteImmediate => Operand.ByteImmediate(ReadS1()),
                OperandType.ShortImmediate => Operand.ShortImmediate(ReadS2()),
                OperandType.BranchOffset => size == 2 ? Operand.BranchOffset(ReadS2()) : Operand.WideBranchOffset(ReadS4()),
                OperandType.NewArrayAtype => Operand.NewArrayAtype(ReadU1()),
                _ => throw new NotSupportedException($"Unsupported operand type: {semanticType}")
            };
        }

        // Fallback: Infer from context (legacy compatibility)
        if (size == 1)
        {
            byte b = ReadU1();
            
            if (IsLocalIndexOp(context)) return Operand.LocalIndex(b);
            if (context == OperationCode.LDC) return Operand.ConstantPoolIndex(b);
            if (context == OperationCode.BIPUSH) return Operand.ByteImmediate((sbyte)b);
            if (context == OperationCode.NEWARRAY) return Operand.NewArrayAtype(b);
            
            return Operand.ByteImmediate((sbyte)b); 
        }
        else if (size == 2)
        {
            if (IsBranchOp(context)) return Operand.BranchOffset(ReadS2());
            if (context == OperationCode.SIPUSH) return Operand.ShortImmediate(ReadS2());
            
            return Operand.ConstantPoolIndex(ReadU2());
        }
        else if (size == 4)
        {
            if (context is OperationCode.GOTO_W or OperationCode.JSR_W)
                return Operand.WideBranchOffset(ReadS4());
            
            throw new NotSupportedException($"Unexpected 4-byte operand for {context}");
        }

        throw new NotSupportedException($"Unsupported operand size: {size} for OpCode {context}");
    }

    // --- Primitive Readers ---

    private byte ReadU1() => _bytes[_pos++];
    
    private sbyte ReadS1() => (sbyte)_bytes[_pos++];

    private ushort ReadU2()
    {
        ushort v = BinaryPrimitives.ReadUInt16BigEndian(_bytes.AsSpan(_pos, 2));
        _pos += 2;
        return v;
    }

    private short ReadS2()
    {
        short v = BinaryPrimitives.ReadInt16BigEndian(_bytes.AsSpan(_pos, 2));
        _pos += 2;
        return v;
    }

    private int ReadS4()
    {
        int v = BinaryPrimitives.ReadInt32BigEndian(_bytes.AsSpan(_pos, 4));
        _pos += 4;
        return v;
    }

    // --- OpCode Classification Helpers ---

    private static bool IsLocalIndexOp(OperationCode op)
    {
        return op is OperationCode.ILOAD or OperationCode.LLOAD or OperationCode.FLOAD 
                  or OperationCode.DLOAD or OperationCode.ALOAD
                  or OperationCode.ISTORE or OperationCode.LSTORE or OperationCode.FSTORE 
                  or OperationCode.DSTORE or OperationCode.ASTORE
                  or OperationCode.RET;
    }

    private static bool IsBranchOp(OperationCode op)
    {
        return (op >= OperationCode.IFEQ && op <= OperationCode.JSR) 
               || op == OperationCode.IFNULL 
               || op == OperationCode.IFNONNULL;
    }
}
