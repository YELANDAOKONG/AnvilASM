namespace Anvil.Instructions.StackMap;

internal class FrameState
{
    public List<JvmType> Locals { get; } = new();
    public List<JvmType> Stack { get; } = new();

    public FrameState Clone()
    {
        var clone = new FrameState();
        clone.Locals.AddRange(Locals);
        clone.Stack.AddRange(Stack);
        return clone;
    }

    public Effect Push(JvmType type)
    {
        Stack.Add(type);
        return Effect.Continue;
    }

    public Effect PushWide(JvmType type)
    {
        Stack.Add(type);
        Stack.Add(JvmType.Top);
        return Effect.Continue;
    }

    public Effect Pop()
    {
        if (Stack.Count > 0)
        {
            Stack.RemoveAt(Stack.Count - 1);
        }

        return Effect.Continue;
    }

    public Effect PopN(int n)
    {
        for (var i = 0; i < n && Stack.Count > 0; i++)
        {
            Stack.RemoveAt(Stack.Count - 1);
        }

        return Effect.Continue;
    }

    public Effect PopWide()
    {
        return PopN(2);
    }

    public Effect PopWide2()
    {
        return PopN(4);
    }
}
