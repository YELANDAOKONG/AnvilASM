namespace Anvil.Instructions;

public class TryCatchBlock
{
    public Label Start { get; set; }
    public Label End { get; set; }
    public Label Handler { get; set; }
    public string? CatchType { get; set; }

    public TryCatchBlock(Label start, Label end, Label handler, string? catchType = null)
    {
        Start = start;
        End = end;
        Handler = handler;
        CatchType = catchType;
    }

    public override string ToString()
    {
        var type = CatchType ?? "finally";
        return $"try [{Start}..{End}) catch({type}) -> {Handler}";
    }
}
