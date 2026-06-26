namespace Anvil.Instructions.StackMap;

internal class JvmType
{
    public JvmKind Kind { get; }
    public virtual string? TypeName => null;
    public virtual int NewOffset => -1;

    protected JvmType(JvmKind kind)
    {
        Kind = kind;
    }

    public static readonly JvmType Int = new(JvmKind.Int);
    public static readonly JvmType Float = new(JvmKind.Float);
    public static readonly JvmType Long = new(JvmKind.Long);
    public static readonly JvmType Double = new(JvmKind.Double);
    public static readonly JvmType Top = new(JvmKind.Top);
    public static readonly JvmType Null = new(JvmKind.Null);

    public override bool Equals(object? obj)
    {
        return obj is JvmType other && other.Kind == Kind;
    }

    public override int GetHashCode()
    {
        return Kind.GetHashCode();
    }
}
