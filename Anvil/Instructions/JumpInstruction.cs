namespace Anvil.Instructions;

public class JumpInstruction : Instruction
{
    private static readonly HashSet<OperationCode> ValidOpCodes =
    [
        OperationCode.IFEQ, OperationCode.IFNE, OperationCode.IFLT, OperationCode.IFGE,
        OperationCode.IFGT, OperationCode.IFLE, OperationCode.IF_ICMPEQ,
        OperationCode.IF_ICMPNE, OperationCode.IF_ICMPLT, OperationCode.IF_ICMPGE,
        OperationCode.IF_ICMPGT, OperationCode.IF_ICMPLE, OperationCode.IF_ACMPEQ,
        OperationCode.IF_ACMPNE, OperationCode.GOTO, OperationCode.JSR,
        OperationCode.IFNULL, OperationCode.IFNONNULL, OperationCode.GOTO_W,
        OperationCode.JSR_W
    ];

    public Label Target { get; set; }

    public int BranchOffset { get; set; }

    public JumpInstruction(Label target) : base(OperationCode.GOTO)
    {
        Target = target;
    }

    public JumpInstruction(OperationCode opCode, Label target) : base(opCode)
    {
        if (!ValidOpCodes.Contains(opCode))
        {
            throw new ArgumentException($"OpCode {opCode} is not a branch instruction.", nameof(opCode));
        }

        Target = target ?? throw new ArgumentNullException(nameof(target));
    }

    public JumpInstruction(OperationCode opCode, string target)
        : this(opCode, new Label(target))
    {
    }

    private bool IsWide => OpCode is OperationCode.GOTO_W or OperationCode.JSR_W;

    public bool NeedsWidening => !IsWide && (BranchOffset < short.MinValue || BranchOffset > short.MaxValue);

    public void UpgradeToWide()
    {
        OpCode = OpCode switch
        {
            OperationCode.GOTO => OperationCode.GOTO_W,
            OperationCode.JSR => OperationCode.JSR_W,
            _ => throw new InvalidOperationException($"Branch {OpCode} has no wide form.")
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
