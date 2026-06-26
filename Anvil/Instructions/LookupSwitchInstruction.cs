namespace Anvil.Instructions;

public class LookupSwitchInstruction : Instruction
{
    public Label DefaultTarget { get; set; }
    public List<(int Key, Label Target)> Pairs { get; set; }

    internal int DefaultOffset { get; set; }
    internal List<(int Key, int Offset)> ResolvedPairs { get; set; } = [];

    public LookupSwitchInstruction(
        Label defaultTarget,
        List<(int Key, Label Target)> pairs) : base(OperationCode.LOOKUPSWITCH)
    {
        DefaultTarget = defaultTarget;
        Pairs = pairs;
    }

    public override int GetSize()
    {
        var pc = Offset ?? 0;
        var padding = (4 - (pc + 1) % 4) % 4;
        return 1 + padding + 8 + Pairs.Count * 8;
    }

    public override void Write(Stream stream)
    {
        stream.WriteByte((byte)OperationCode.LOOKUPSWITCH);

        var padding = (4 - (Offset!.Value + 1) % 4) % 4;
        for (var i = 0; i < padding; i++)
        {
            stream.WriteByte(0);
        }

        WriteInt32(stream, DefaultOffset);
        WriteInt32(stream, Pairs.Count);

        foreach (var (key, offset) in ResolvedPairs)
        {
            WriteInt32(stream, key);
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
        var pairs = string.Join(", ", Pairs.Select(p => $"{p.Key}->{p.Target}"));
        return $"{OpCode} default={DefaultTarget} pairs=[{pairs}]";
    }
}
