using Anvil.Interfaces;
using Anvil.Structures.Attributes;

namespace Anvil.Factories;

public static class AttributeFactory
{
    /// <summary>
    /// Parses the raw bytes of an attribute into a specific IAttribute implementation.
    /// </summary>
    /// <param name="name">The resolved name of the attribute (e.g., "Code", "InnerClasses").</param>
    /// <param name="data">The raw info[] bytes.</param>
    /// <returns>A specific implementation of IAttribute, or null if unknown/unsupported.</returns>
    public static IAttribute? Create(string name, byte[] data)
    {
        // For variable length attributes that consume the whole array without internal length prefixes 
        // (like SourceDebugExtension), we pass the array directly.
        // For structured attributes, we wrap in a MemoryStream.
        
        using var stream = new MemoryStream(data);

        return name switch
        {
            "ConstantValue" => ConstantValueAttribute.Read(stream),
            "Code" => CodeAttribute.Read(stream),
            "StackMapTable" => StackMapTableAttribute.Read(stream),
            "Exceptions" => ExceptionsAttribute.Read(stream),
            "InnerClasses" => InnerClassesAttribute.Read(stream),
            "EnclosingMethod" => EnclosingMethodAttribute.Read(stream),
            "Synthetic" => SyntheticAttribute.Read(stream),
            "Signature" => SignatureAttribute.Read(stream),
            "SourceFile" => SourceFileAttribute.Read(stream),
            "SourceDebugExtension" => SourceDebugExtensionAttribute.Read(data),
            "LineNumberTable" => LineNumberTableAttribute.Read(stream),
            "LocalVariableTable" => LocalVariableTableAttribute.Read(stream),
            "LocalVariableTypeTable" => LocalVariableTypeTableAttribute.Read(stream),
            "Deprecated" => DeprecatedAttribute.Read(stream),
            "RuntimeVisibleAnnotations" => RuntimeVisibleAnnotationsAttribute.Read(stream),
            "RuntimeInvisibleAnnotations" => RuntimeInvisibleAnnotationsAttribute.Read(stream),
            "RuntimeVisibleParameterAnnotations" => RuntimeVisibleParameterAnnotationsAttribute.Read(stream),
            "RuntimeInvisibleParameterAnnotations" => RuntimeInvisibleParameterAnnotationsAttribute.Read(stream),
            "RuntimeVisibleTypeAnnotations" => RuntimeVisibleTypeAnnotationsAttribute.Read(stream),
            "RuntimeInvisibleTypeAnnotations" => RuntimeInvisibleTypeAnnotationsAttribute.Read(stream),
            "AnnotationDefault" => AnnotationDefaultAttribute.Read(stream),
            "BootstrapMethods" => BootstrapMethodsAttribute.Read(stream),
            "MethodParameters" => MethodParametersAttribute.Read(stream),
            "Module" => ModuleAttribute.Read(stream),
            "ModulePackages" => ModulePackagesAttribute.Read(stream),
            "ModuleMainClass" => ModuleMainClassAttribute.Read(stream),
            "NestHost" => NestHostAttribute.Read(stream),
            "NestMembers" => NestMembersAttribute.Read(stream),
            "Record" => RecordAttribute.Read(stream),
            "PermittedSubclasses" => PermittedSubclassesAttribute.Read(stream),
            
            _ => null
        };
    }
}
