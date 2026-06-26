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
        builder.Interfaces = node.Interfaces.ToList();

        foreach (var field in node.Fields)
        {
            builder.AddField(field.Name, field.Descriptor, field.AccessFlags);
        }

        foreach (var method in node.Methods)
        {
            var entry = builder.AddMethod(method.Name, method.Descriptor, method.AccessFlags);
            entry.Body = method.Body;
        }

        builder.Write(stream);
    }
}
