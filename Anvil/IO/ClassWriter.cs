using Anvil.Core;
using Anvil.Models;

namespace Anvil.IO;

public static class ClassWriter
{
    public static void Write(ClassNode node, Stream stream)
    {
        var builder = ClassBuilder.Create(node.MajorVersion, node.MinorVersion);
        builder.ClassFile.AccessFlags = node.AccessFlags;
        builder.Name = node.Name;
        builder.SuperName = node.SuperName;
        builder.Signature = node.Signature;
        builder.SourceFile = node.SourceFile;
        builder.Interfaces = node.Interfaces.ToList();
        builder.Attributes = node.Attributes.ToList();

        foreach (var field in node.Fields)
        {
            var entry = builder.AddField(field.Name, field.Descriptor, field.AccessFlags);
            entry.Signature = field.Signature;
            entry.Attributes = field.Attributes.ToList();
        }

        foreach (var method in node.Methods)
        {
            var entry = builder.AddMethod(method.Name, method.Descriptor, method.AccessFlags);
            entry.Signature = method.Signature;
            entry.Exceptions = method.Exceptions.ToList();
            entry.Attributes = method.Attributes.ToList();
            entry.Body = method.Body;
        }

        builder.Write(stream);
    }
}
