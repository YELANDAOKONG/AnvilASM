namespace Anvil.Instructions;

public class BytecodeLocalVariable
{
    public Label Start { get; set; }
    public Label End { get; set; }
    public string Name { get; set; }
    public string? Descriptor { get; set; }
    public string? Signature { get; set; }
    public int Index { get; set; }

    public BytecodeLocalVariable(
        Label start,
        Label end,
        string name,
        string? descriptor,
        int index,
        string? signature = null)
    {
        Start = start ?? throw new ArgumentNullException(nameof(start));
        End = end ?? throw new ArgumentNullException(nameof(end));
        Name = string.IsNullOrEmpty(name)
            ? throw new ArgumentException("Local variable name cannot be empty.", nameof(name))
            : name;
        Descriptor = descriptor;
        Signature = signature;

        if (index < 0 || index > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                "Local variable index must fit in an unsigned 16-bit value.");
        }

        Index = index;
    }

    public BytecodeLocalVariable(
        string start,
        string end,
        string name,
        string? descriptor,
        int index,
        string? signature = null)
        : this(
            new Label(start),
            new Label(end),
            name,
            descriptor,
            index,
            signature)
    {
    }
}
