using System.Text;

namespace Anvil.Descriptors;

/// <summary>
/// Represents a Field Descriptor or a Return Descriptor.
/// Spec §4.3.2
/// </summary>
public class TypeDescriptor
{
    /// <summary>
    /// The specific kind of type (Primitive, Object, Array, or Void).
    /// </summary>
    public DescriptorTag Tag { get; }

    /// <summary>
    /// The number of array dimensions. 0 if not an array.
    /// </summary>
    public int ArrayRank { get; }

    /// <summary>
    /// The binary name of the class in internal form (e.g., "java/lang/String").
    /// Only populated if Tag is Object.
    /// </summary>
    public string? InternalName { get; }

    /// <summary>
    /// If this is an Array, represents the type of the element.
    /// If this is not an array, returns 'this'.
    /// </summary>
    public TypeDescriptor ElementType { get; }

    private TypeDescriptor(DescriptorTag tag, string? internalName, int arrayRank, TypeDescriptor? elementType)
    {
        Tag = tag;
        InternalName = internalName;
        ArrayRank = arrayRank;
        ElementType = elementType ?? this;
    }

    /// <summary>
    /// Creates a primitive or void descriptor.
    /// </summary>
    public static TypeDescriptor CreatePrimitive(DescriptorTag tag)
    {
        if (tag == DescriptorTag.Object || tag == DescriptorTag.Array)
            throw new ArgumentException("Use specific factory methods for Object or Array types.", nameof(tag));
            
        return new TypeDescriptor(tag, null, 0, null);
    }

    /// <summary>
    /// Creates an object descriptor (LClassName;).
    /// </summary>
    /// <param name="internalName">The internal name (e.g. java/lang/Object).</param>
    public static TypeDescriptor CreateObject(string internalName)
    {
        if (string.IsNullOrEmpty(internalName))
            throw new ArgumentException("Internal name cannot be null or empty.", nameof(internalName));

        return new TypeDescriptor(DescriptorTag.Object, internalName, 0, null);
    }

    /// <summary>
    /// Creates an array descriptor ([ComponentType).
    /// </summary>
    public static TypeDescriptor CreateArray(TypeDescriptor componentType)
    {
        // If adding a dimension to an existing array, increase rank but keep the base element type
        if (componentType.IsArray)
        {
            return new TypeDescriptor(
                DescriptorTag.Array, 
                componentType.InternalName, 
                componentType.ArrayRank + 1, 
                componentType.ElementType);
        }

        return new TypeDescriptor(DescriptorTag.Array, componentType.InternalName, 1, componentType);
    }

    public bool IsArray => Tag == DescriptorTag.Array;
    public bool IsObject => Tag == DescriptorTag.Object;
    public bool IsPrimitive => !IsArray && !IsObject && Tag != DescriptorTag.Void;

    /// <summary>
    /// Returns the full descriptor string (e.g., "[[Ljava/lang/String;").
    /// </summary>
    public override string ToString()
    {
        var sb = new StringBuilder();
        
        for (int i = 0; i < ArrayRank; i++) sb.Append('[');

        var baseType = IsArray ? ElementType : this;

        switch (baseType.Tag)
        {
            case DescriptorTag.Byte:    sb.Append('B'); break;
            case DescriptorTag.Char:    sb.Append('C'); break;
            case DescriptorTag.Double:  sb.Append('D'); break;
            case DescriptorTag.Float:   sb.Append('F'); break;
            case DescriptorTag.Int:     sb.Append('I'); break;
            case DescriptorTag.Long:    sb.Append('J'); break;
            case DescriptorTag.Short:   sb.Append('S'); break;
            case DescriptorTag.Boolean: sb.Append('Z'); break;
            case DescriptorTag.Void:    sb.Append('V'); break;
            case DescriptorTag.Object:
                sb.Append('L').Append(baseType.InternalName).Append(';');
                break;
        }

        return sb.ToString();
    }
}
