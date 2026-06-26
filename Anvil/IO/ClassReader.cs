using Anvil.Core;
using Anvil.Models;

namespace Anvil.IO;

public static class ClassReader
{
    public static ClassNode Read(Stream stream)
    {
        var builder = ClassBuilder.Read(stream);
        var node = new ClassNode
        {
            MajorVersion = builder.ClassFile.MajorVersion.Value,
            MinorVersion = builder.ClassFile.MinorVersion.Value,
            AccessFlags = builder.ClassFile.AccessFlags,
            Name = builder.Name,
            SuperName = builder.SuperName,
            Interfaces = builder.Interfaces.ToList()
        };

        foreach (var field in builder.Fields)
        {
            node.Fields.Add(new FieldNode(field.Info.AccessFlags, field.Name, field.Descriptor));
        }

        foreach (var method in builder.Methods)
        {
            node.Methods.Add(new MethodNode(method.Info.AccessFlags, method.Name, method.Descriptor, method.Body));
        }

        return node;
    }
}
