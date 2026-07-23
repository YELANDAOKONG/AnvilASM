using Anvil.Instructions.ConstantPool;

namespace Anvil.Instructions;

public class TypeInstruction : Instruction
{
    public string? Type { get; set; }

    public int TypeIndex { get; set; }

    public TypeInstruction(OperationCode opCode, string type) : base(opCode)
    {
        ValidateOpCode(opCode);
        Type = type;
    }

    public TypeInstruction(OperationCode opCode, int typeIndex) : base(opCode)
    {
        ValidateOpCode(opCode);
        TypeIndex = typeIndex;
    }

    private static void ValidateOpCode(OperationCode opCode)
    {
        if (opCode is not (OperationCode.NEW
            or OperationCode.ANEWARRAY
            or OperationCode.CHECKCAST
            or OperationCode.INSTANCEOF))
        {
            throw new ArgumentException($"OpCode {opCode} is not a type instruction.", nameof(opCode));
        }
    }

    internal void Resolve(ConstantPoolBuilder cp)
    {
        if (Type is null)
        {
            return;
        }

        TypeIndex = cp.AddClass(Type);
    }

    public override int GetSize() => 3;

    public override void Write(Stream stream)
    {
        stream.WriteByte((byte)OpCode);
        stream.WriteByte((byte)((TypeIndex >> 8) & 0xFF));
        stream.WriteByte((byte)(TypeIndex & 0xFF));
    }

    public override string ToString()
    {
        if (Type != null)
        {
            return $"{OpCode} {Type}";
        }

        return $"{OpCode} #{TypeIndex}";
    }
}
