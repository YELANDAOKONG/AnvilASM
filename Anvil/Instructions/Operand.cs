using System;

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
        return BitConverter.ToString(Data).Replace("-", "");
    }

    // Factory methods
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
        if (index <= 0xFF && inc >= sbyte.MinValue && inc <= sbyte.MaxValue)
            return new(OperandType.IincPair, (byte)index, (byte)inc);
        return new(OperandType.IincPair, (byte)(index >> 8), (byte)index, (byte)(inc >> 8), (byte)inc);
    }

    public static Operand InvokeInterface(ushort methodIndex, byte argsCount)
        => new(OperandType.InvokeInterfaceArgs, (byte)(methodIndex >> 8), (byte)methodIndex, argsCount, 0);

    public static Operand MultiANewArray(ushort classIndex, byte dimensions)
        => new(OperandType.MultiANewArrayArgs, (byte)(classIndex >> 8), (byte)classIndex, dimensions);

    public static Operand NewArrayAtype(byte atype)
        => new(OperandType.NewArrayAtype, atype);
}