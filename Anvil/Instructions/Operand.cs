using System;
using System.Buffers.Binary;

namespace Anvil.Instructions;

public class Operand
{
    public OperandType Type { get; }
    public byte[] Data { get; }

    private Operand(OperandType type, params byte[] data)
    {
        Type = type;
        Data = data ?? Array.Empty<byte>();
    }

    public override string ToString()
    {
        return Type switch
        {
            OperandType.TableSwitchData => "tableswitch...",
            OperandType.LookupSwitchData => "lookupswitch...",
            _ => BitConverter.ToString(Data).Replace("-", "")
        };
    }

    // ================== Factory Methods ==================

    public static Operand LocalIndex(byte index) => new(OperandType.LocalIndex, index);
    public static Operand WideLocalIndex(ushort index) => new(OperandType.LocalIndex, (byte)(index >> 8), (byte)index);

    public static Operand ConstantPoolIndex(byte index) => new(OperandType.ConstantPoolIndex, index);
    public static Operand ConstantPoolIndex(ushort index) => new(OperandType.ConstantPoolIndex, (byte)(index >> 8), (byte)index);

    public static Operand ByteImmediate(sbyte value) => new(OperandType.ByteImmediate, (byte)value);
    public static Operand ShortImmediate(short value) => new(OperandType.ShortImmediate, (byte)(value >> 8), (byte)value);

    public static Operand BranchOffset(short offset) => new(OperandType.BranchOffset, (byte)(offset >> 8), (byte)offset);
    public static Operand WideBranchOffset(int offset) => new(OperandType.BranchOffset,
        (byte)(offset >> 24), (byte)(offset >> 16), (byte)(offset >> 8), (byte)offset);

    public static Operand Iinc(ushort index, short inc)
    {
        // Standard: index (1 byte), const (1 byte)
        if (index <= 0xFF && inc >= sbyte.MinValue && inc <= sbyte.MaxValue)
            return new(OperandType.IincPair, (byte)index, (byte)inc);
        
        // Wide: index (2 bytes), const (2 bytes)
        return new(OperandType.IincPair, (byte)(index >> 8), (byte)index, (byte)(inc >> 8), (byte)inc);
    }

    public static Operand InvokeInterface(ushort methodIndex, byte argsCount)
        => new(OperandType.InvokeInterfaceArgs, (byte)(methodIndex >> 8), (byte)methodIndex, argsCount, 0);

    public static Operand MultiANewArray(ushort classIndex, byte dimensions)
        => new(OperandType.MultiANewArrayArgs, (byte)(classIndex >> 8), (byte)classIndex, dimensions);

    public static Operand NewArrayAtype(byte atype)
        => new(OperandType.NewArrayAtype, atype);
    
    public static Operand TableSwitch(int defaultOffset, int low, int high, int[] offsets)
    {
        if (offsets.Length != (high - low + 1))
            throw new ArgumentException("Offset count must match high - low + 1");

        // Layout: default(4) + low(4) + high(4) + offsets(4 * N)
        // Note: Padding bytes are NOT stored here, they are calculated during write.
        byte[] data = new byte[12 + offsets.Length * 4];
        var span = data.AsSpan();

        BinaryPrimitives.WriteInt32BigEndian(span[0..4], defaultOffset);
        BinaryPrimitives.WriteInt32BigEndian(span[4..8], low);
        BinaryPrimitives.WriteInt32BigEndian(span[8..12], high);

        for (int i = 0; i < offsets.Length; i++)
        {
            BinaryPrimitives.WriteInt32BigEndian(span[(12 + i * 4)..], offsets[i]);
        }

        return new(OperandType.TableSwitchData, data);
    }

    public static Operand LookupSwitch(int defaultOffset, (int match, int offset)[] pairs)
    {
        // Sort pairs by match key as required by JVM spec
        Array.Sort(pairs, (a, b) => a.match.CompareTo(b.match));

        // Layout: default(4) + npairs(4) + pairs(8 * N)
        byte[] data = new byte[8 + pairs.Length * 8];
        var span = data.AsSpan();

        BinaryPrimitives.WriteInt32BigEndian(span[0..4], defaultOffset);
        BinaryPrimitives.WriteInt32BigEndian(span[4..8], pairs.Length);

        for (int i = 0; i < pairs.Length; i++)
        {
            int baseIdx = 8 + i * 8;
            BinaryPrimitives.WriteInt32BigEndian(span[baseIdx..], pairs[i].match);
            BinaryPrimitives.WriteInt32BigEndian(span[(baseIdx + 4)..], pairs[i].offset);
        }

        return new(OperandType.LookupSwitchData, data);
    }
}
