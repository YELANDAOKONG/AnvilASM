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

    public void Push(JvmType type)
    {
        Stack.Add(type);
    }

    public JvmType Peek()
    {
        if (Stack.Count == 0)
        {
            throw new InvalidOperationException("Operand stack underflow.");
        }

        return Stack[^1];
    }

    public JvmType Pop()
    {
        if (Stack.Count == 0)
        {
            throw new InvalidOperationException("Operand stack underflow.");
        }

        var value = Stack[^1];
        Stack.RemoveAt(Stack.Count - 1);
        return value;
    }

    public void Pop(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (Stack.Count < count)
        {
            throw new InvalidOperationException("Operand stack underflow.");
        }

        Stack.RemoveRange(Stack.Count - count, count);
    }

    public void SetLocal(int index, JvmType type)
    {
        while (Locals.Count <= index)
        {
            Locals.Add(JvmType.Top);
        }

        Locals[index] = type;
    }

    public void Replace(JvmType oldType, JvmType newType)
    {
        for (var i = 0; i < Locals.Count; i++)
        {
            if (Locals[i].Equals(oldType))
            {
                Locals[i] = newType;
            }
        }

        for (var i = 0; i < Stack.Count; i++)
        {
            if (Stack[i].Equals(oldType))
            {
                Stack[i] = newType;
            }
        }
    }
}
