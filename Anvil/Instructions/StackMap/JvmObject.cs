namespace Anvil.Instructions.StackMap;

internal class JvmObject : JvmType
{
    public override string TypeName { get; }

    public JvmObject(string typeName) : base(JvmKind.Object)
    {
        TypeName = typeName;
    }

    public override bool Equals(object? obj)
    {
        return obj is JvmObject other && other.TypeName == TypeName;
    }

    public override int GetHashCode()
    {
        return TypeName.GetHashCode();
    }
}
