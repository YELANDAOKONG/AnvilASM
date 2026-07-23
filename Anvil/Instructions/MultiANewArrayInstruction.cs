using Anvil.Instructions.ConstantPool;

namespace Anvil.Instructions;

public class MultiANewArrayInstruction : Instruction
{
    public string? Type { get; set; }
    public int Dimensions { get; set; }

    public int TypeIndex { get; set; }

    public MultiANewArrayInstruction(string type, int dimensions) : base(OperationCode.MULTIANEWARRAY)
    {
        ValidateDimensions(dimensions);
        Type = type;
        Dimensions = dimensions;
    }

    public MultiANewArrayInstruction(int typeIndex, int dimensions) : base(OperationCode.MULTIANEWARRAY)
    {
        ValidateDimensions(dimensions);
        TypeIndex = typeIndex;
        Dimensions = dimensions;
    }

    private static void ValidateDimensions(int dimensions)
    {
        if (dimensions is <= 0 or > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dimensions),
                dimensions,
                "Array dimensions must be between 1 and 255.");
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
