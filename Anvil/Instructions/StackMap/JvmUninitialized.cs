namespace Anvil.Instructions.StackMap;

internal class JvmUninitialized : JvmType
{
    public override int NewOffset { get; }

    public JvmUninitialized(int newOffset) : base(JvmKind.Uninitialized)
    {
        NewOffset = newOffset;
    }

    public override bool Equals(object? obj)
    {
        return obj is JvmUninitialized other && other.NewOffset == NewOffset;
    }

    public override int GetHashCode()
    {
        return NewOffset.GetHashCode();
    }
}
