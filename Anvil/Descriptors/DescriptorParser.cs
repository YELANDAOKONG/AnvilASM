namespace Anvil.Descriptors;

/// <summary>
/// Provides methods to parse JVM descriptor strings.
/// </summary>
public static class DescriptorParser
{
    public static TypeDescriptor ParseType(string descriptor)
    {
        if (string.IsNullOrEmpty(descriptor))
            throw new ArgumentException("Descriptor cannot be null or empty.", nameof(descriptor));

        int index = 0;
        return ParseTypeRecursive(descriptor, ref index);
    }

    public static MethodDescriptor ParseMethod(string descriptor)
    {
        if (string.IsNullOrEmpty(descriptor))
            throw new ArgumentException("Descriptor cannot be null or empty.", nameof(descriptor));

        if (descriptor[0] != '(')
            throw new FormatException($"Invalid method descriptor start: {descriptor}");

        var parameters = new List<TypeDescriptor>();
        int index = 1; // Skip '('

        while (index < descriptor.Length && descriptor[index] != ')')
        {
            parameters.Add(ParseTypeRecursive(descriptor, ref index));
        }

        if (index >= descriptor.Length || descriptor[index] != ')')
            throw new FormatException("Invalid method descriptor: missing closing parenthesis.");

        index++; // Skip ')'
        
        var returnType = ParseTypeRecursive(descriptor, ref index);

        return new MethodDescriptor(parameters.ToArray(), returnType);
    }

    private static TypeDescriptor ParseTypeRecursive(string descriptor, ref int index)
    {
        if (index >= descriptor.Length)
            throw new FormatException("Unexpected end of descriptor.");

        char c = descriptor[index++];

        switch (c)
        {
            case 'B': return TypeDescriptor.CreatePrimitive(DescriptorTag.Byte);
            case 'C': return TypeDescriptor.CreatePrimitive(DescriptorTag.Char);
            case 'D': return TypeDescriptor.CreatePrimitive(DescriptorTag.Double);
            case 'F': return TypeDescriptor.CreatePrimitive(DescriptorTag.Float);
            case 'I': return TypeDescriptor.CreatePrimitive(DescriptorTag.Int);
            case 'J': return TypeDescriptor.CreatePrimitive(DescriptorTag.Long);
            case 'S': return TypeDescriptor.CreatePrimitive(DescriptorTag.Short);
            case 'Z': return TypeDescriptor.CreatePrimitive(DescriptorTag.Boolean);
            case 'V': return TypeDescriptor.CreatePrimitive(DescriptorTag.Void);
            
            case 'L':
                int semiColonIndex = descriptor.IndexOf(';', index);
                if (semiColonIndex == -1)
                    throw new FormatException($"Invalid object descriptor at index {index}: missing semicolon.");
                
                // Extract "java/lang/String" from "Ljava/lang/String;"
                string internalName = descriptor.Substring(index, semiColonIndex - index);
                index = semiColonIndex + 1;
                return TypeDescriptor.CreateObject(internalName);

            case '[':
                // Recursively parse the component type
                var componentType = ParseTypeRecursive(descriptor, ref index);
                return TypeDescriptor.CreateArray(componentType);

            default:
                throw new FormatException($"Invalid descriptor character '{c}' at index {index - 1}.");
        }
    }
}
