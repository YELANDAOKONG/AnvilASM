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
            Signature = builder.Signature,
            SourceFile = builder.SourceFile,
            Interfaces = builder.Interfaces.ToList(),
            Attributes = builder.Attributes.ToList()
        };

        foreach (var field in builder.Fields)
        {
            var fn = new FieldNode(field.Info.AccessFlags, field.Name, field.Descriptor)
            {
                Signature = field.Signature,
                Attributes = field.Attributes.ToList()
            };
            node.Fields.Add(fn);
        }

        foreach (var method in builder.Methods)
        {
            var mn = new MethodNode(method.Info.AccessFlags, method.Name, method.Descriptor, method.Body)
            {
                Signature = method.Signature,
                Exceptions = method.Exceptions.ToList(),
                Attributes = method.Attributes.ToList()
            };
            node.Methods.Add(mn);
        }

        return node;
    }
}
