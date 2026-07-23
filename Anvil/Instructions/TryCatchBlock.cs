namespace Anvil.Instructions;

public class TryCatchBlock
{
    public Label Start { get; set; }
    public Label End { get; set; }
    public Label Handler { get; set; }
    public string? CatchType { get; set; }

    public TryCatchBlock(Label start, Label end, Label handler, string? catchType = null)
    {
        Start = start ?? throw new ArgumentNullException(nameof(start));
        End = end ?? throw new ArgumentNullException(nameof(end));
        Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        CatchType = catchType;
    }

    public TryCatchBlock(string start, string end, string handler, string? catchType = null)
        : this(new Label(start), new Label(end), new Label(handler), catchType)
    {
    }

    public override string ToString()
    {
        var type = CatchType ?? "finally";
        return $"try [{Start}..{End}) catch({type}) -> {Handler}";
    }
}
