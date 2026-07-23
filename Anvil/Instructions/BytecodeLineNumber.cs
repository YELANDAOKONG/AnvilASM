namespace Anvil.Instructions;

public class BytecodeLineNumber
{
    public Label Start { get; set; }
    public int LineNumber { get; set; }

    public BytecodeLineNumber(Label start, int lineNumber)
    {
        Start = start ?? throw new ArgumentNullException(nameof(start));
        if (lineNumber < 0 || lineNumber > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lineNumber),
                lineNumber,
                "Line number must fit in an unsigned 16-bit value.");
        }

        LineNumber = lineNumber;
    }

    public BytecodeLineNumber(string start, int lineNumber)
        : this(new Label(start), lineNumber)
    {
    }
}
