using System.Text;

namespace Anvil.Descriptors;

/// <summary>
/// Represents a Method Descriptor.
/// Spec §4.3.3
/// </summary>
public class MethodDescriptor
{
    public TypeDescriptor[] Parameters { get; }
    public TypeDescriptor ReturnType { get; }

    public MethodDescriptor(TypeDescriptor[] parameters, TypeDescriptor returnType)
    {
        Parameters = parameters ?? Array.Empty<TypeDescriptor>();
        ReturnType = returnType ?? throw new ArgumentNullException(nameof(returnType));
    }

    /// <summary>
    /// Calculates the size of the parameters in local variable units.
    /// Long and Double take 2 units; others take 1.
    /// Does not include 'this'.
    /// </summary>
    public int ComputeSize()
    {
        int size = 0;
        foreach (var param in Parameters)
        {
            if (param.Tag == DescriptorTag.Long || param.Tag == DescriptorTag.Double)
                size += 2;
            else
                size += 1;
        }
        return size;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append('(');
        foreach (var param in Parameters)
        {
            sb.Append(param.ToString());
        }
        sb.Append(')');
        sb.Append(ReturnType.ToString());
        return sb.ToString();
    }
}