namespace Anvil.Instructions;

public class Label
{
    private static int _nextId;

    public string? Name { get; }

    internal int Id { get; }

    internal int? Offset { get; set; }

    public Label(string? name = null)
    {
        Id = Interlocked.Increment(ref _nextId);
        Name = name;
    }

    public override string ToString() => Name ?? $"L{Id:X4}";
}
