namespace Anvil.Instructions.StackMap;

internal class JvmUninitializedThis : JvmType
{
    public JvmUninitializedThis() : base(JvmKind.UninitializedThis)
    {
    }

    public override bool Equals(object? obj)
    {
        return obj is JvmUninitializedThis;
    }

    public override int GetHashCode()
    {
        return 0xCAFE;
    }
}
