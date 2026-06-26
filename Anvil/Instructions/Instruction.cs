namespace Anvil.Instructions;

public abstract class Instruction
{
    public OperationCode OpCode { get; }

    public List<Label> Labels { get; } = [];

    internal int? Offset { get; set; }

    protected Instruction(OperationCode opCode)
    {
        OpCode = opCode;
    }

    public abstract int GetSize();

    public abstract void Write(Stream stream);

    public override string ToString()
    {
        var labels = Labels.Count > 0
            ? $" [{string.Join(", ", Labels)}]"
            : string.Empty;
        return $"{OpCode}{labels}";
    }
}
