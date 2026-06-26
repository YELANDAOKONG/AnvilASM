namespace Anvil.Instructions.StackMap;

internal struct Effect
{
    public bool KeepGoing { get; }

    public static Effect Continue => new(true);
    public static Effect Stop => new(false);

    private Effect(bool cont)
    {
        KeepGoing = cont;
    }

    public static implicit operator Effect(bool value)
    {
        return new Effect(value);
    }

    public static Effect operator &(Effect a, Effect b)
    {
        return a.KeepGoing && b.KeepGoing ? Continue : Stop;
    }

    public static bool operator true(Effect e)
    {
        return e.KeepGoing;
    }

    public static bool operator false(Effect e)
    {
        return !e.KeepGoing;
    }
}
