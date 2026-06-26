namespace Anvil.Instructions;

public class JumpInstruction : Instruction
{
    public Label Target { get; set; }

    internal int BranchOffset { get; set; }

    public JumpInstruction(OperationCode opCode, Label target) : base(opCode)
    {
        Target = target;
    }

    private bool IsWide => OpCode is OperationCode.GOTO_W or OperationCode.JSR_W;

    public override int GetSize() => IsWide ? 5 : 3;

    public override void Write(Stream stream)
    {
        if (!IsWide && (BranchOffset < short.MinValue || BranchOffset > short.MaxValue))
        {
            throw new InvalidOperationException(
                $"Branch offset {BranchOffset} does not fit in signed 16-bit. Use GOTO_W/JSR_W instead of {OpCode}.");
        }

        stream.WriteByte((byte)OpCode);

        if (IsWide)
        {
            stream.WriteByte((byte)((BranchOffset >> 24) & 0xFF));
            stream.WriteByte((byte)((BranchOffset >> 16) & 0xFF));
        }

        stream.WriteByte((byte)((BranchOffset >> 8) & 0xFF));
        stream.WriteByte((byte)(BranchOffset & 0xFF));
    }

    public override string ToString() => $"{OpCode} -> {Target}";
}
