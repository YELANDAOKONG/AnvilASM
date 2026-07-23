namespace Anvil.Instructions;

public class TableSwitchInstruction : Instruction
{
    public int Low { get; set; }
    public int High { get; set; }
    public Label DefaultTarget { get; set; }
    public List<Label> Targets { get; set; }

    internal int DefaultOffset { get; set; }
    internal List<int> TargetOffsets { get; set; } = [];

    public TableSwitchInstruction(
        int low,
        int high,
        Label defaultTarget,
        List<Label> targets) : base(OperationCode.TABLESWITCH)
    {
        ArgumentNullException.ThrowIfNull(defaultTarget);
        ArgumentNullException.ThrowIfNull(targets);

        if (high < low)
        {
            throw new ArgumentOutOfRangeException(nameof(high), "High must be greater than or equal to low.");
        }

        if ((long)high - low + 1 != targets.Count)
        {
            throw new ArgumentException("Target count must equal high - low + 1.", nameof(targets));
        }

        Low = low;
        High = high;
        DefaultTarget = defaultTarget;
        Targets = targets;
    }

    public TableSwitchInstruction(
        int low,
        int high,
        string defaultTarget,
        IEnumerable<string> targets)
        : this(low, high, new Label(defaultTarget), targets.Select(name => new Label(name)).ToList())
    {
    }

    public override int GetSize()
    {
        var pc = Offset ?? 0;
        var padding = (4 - (pc + 1) % 4) % 4;
        return 1 + padding + 12 + Targets.Count * 4;
    }

    public override void Write(Stream stream)
    {
        stream.WriteByte((byte)OperationCode.TABLESWITCH);

        var padding = (4 - (Offset!.Value + 1) % 4) % 4;
        for (var i = 0; i < padding; i++)
        {
            stream.WriteByte(0);
        }

        WriteInt32(stream, DefaultOffset);
        WriteInt32(stream, Low);
        WriteInt32(stream, High);

        foreach (var offset in TargetOffsets)
        {
            WriteInt32(stream, offset);
        }
    }

    private static void WriteInt32(Stream stream, int value)
    {
        stream.WriteByte((byte)((value >> 24) & 0xFF));
        stream.WriteByte((byte)((value >> 16) & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
        stream.WriteByte((byte)(value & 0xFF));
    }

    public override string ToString()
    {
        var targets = string.Join(", ", Targets.Select(t => t.ToString()));
        return $"{OpCode} low={Low} high={High} default={DefaultTarget} targets=[{targets}]";
    }
}
