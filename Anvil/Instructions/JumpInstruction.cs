namespace Anvil.Instructions;

public class JumpInstruction : Instruction
{
    public Label Target { get; set; }

    public int BranchOffset { get; set; }

    public JumpInstruction(Label target) : base(OperationCode.GOTO)
    {
        Target = target;
    }

    public JumpInstruction(OperationCode opCode, Label target) : base(opCode)
    {
        Target = target;
    }

    private bool IsWide => OpCode is OperationCode.GOTO_W or OperationCode.JSR_W;

    public bool NeedsWidening => !IsWide && (BranchOffset < short.MinValue || BranchOffset > short.MaxValue);

    public void UpgradeToWide()
    {
        OpCode = OpCode switch
        {
            OperationCode.GOTO => OperationCode.GOTO_W,
            OperationCode.JSR => OperationCode.JSR_W,
            _ => OpCode
        };
    }

    public override int GetSize() => IsWide ? 5 : 3;

    public override void Write(Stream stream)
    {
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
