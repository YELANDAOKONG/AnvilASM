using Anvil.Instructions.ConstantPool;

namespace Anvil.Instructions;

public class MultiANewArrayInstruction : Instruction
{
    public string? Type { get; set; }
    public int Dimensions { get; set; }

    public int TypeIndex { get; set; }

    private bool _resolved;

    public MultiANewArrayInstruction(string type, int dimensions) : base(OperationCode.MULTIANEWARRAY)
    {
        Type = type;
        Dimensions = dimensions;
    }

    public MultiANewArrayInstruction(int typeIndex, int dimensions) : base(OperationCode.MULTIANEWARRAY)
    {
        TypeIndex = typeIndex;
        Dimensions = dimensions;
        _resolved = true;
    }

    internal void Resolve(ConstantPoolBuilder cp)
    {
        if (_resolved)
        {
            return;
        }

        TypeIndex = cp.AddClass(Type!);
        _resolved = true;
    }

    public override int GetSize() => 4;

    public override void Write(Stream stream)
    {
        stream.WriteByte((byte)OperationCode.MULTIANEWARRAY);
        stream.WriteByte((byte)((TypeIndex >> 8) & 0xFF));
        stream.WriteByte((byte)(TypeIndex & 0xFF));
        stream.WriteByte((byte)Dimensions);
    }

    public override string ToString()
    {
        if (Type != null)
        {
            return $"{OpCode} {Type} dims={Dimensions}";
        }

        return $"{OpCode} #{TypeIndex} {Dimensions}";
    }
}
