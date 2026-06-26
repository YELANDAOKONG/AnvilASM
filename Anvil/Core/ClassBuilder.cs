using Anvil.Instructions;
using Anvil.Instructions.ConstantPool;
using Anvil.Structures;
using Anvil.Structures.Attributes;
using Anvil.Structures.ConstantPool;
using Anvil.Types;

namespace Anvil.Core;

public class MethodEntry
{
    public MethodInfo Info { get; }
    public string Name { get; }
    public string Descriptor { get; }
    public MethodBody? Body { get; set; }

    internal MethodEntry(MethodInfo info, string name, string descriptor, MethodBody? body)
    {
        Info = info;
        Name = name;
        Descriptor = descriptor;
        Body = body;
    }
}

public class FieldEntry
{
    public FieldInfo Info { get; }
    public string Name { get; }
    public string Descriptor { get; }

    internal FieldEntry(FieldInfo info, string name, string descriptor)
    {
        Info = info;
        Name = name;
        Descriptor = descriptor;
    }
}

public class ClassBuilder
{
    public ClassFile ClassFile { get; private set; }
    private readonly List<FieldEntry> _fields = [];
    private readonly List<MethodEntry> _methods = [];

    private ClassBuilder(ClassFile classFile)
    {
        ClassFile = classFile;
    }

    public IReadOnlyList<FieldEntry> Fields => _fields;
    public IReadOnlyList<MethodEntry> Methods => _methods;

    public static ClassBuilder Create(int majorVersion = 65, int minorVersion = 0)
    {
        var classFile = new ClassFile
        {
            Magic = new TUInt(ClassFile.MagicNumber),
            MinorVersion = new TUShort((ushort)minorVersion),
            MajorVersion = new TUShort((ushort)majorVersion),
            ConstantPoolCount = new TUShort(1),
            ConstantPool = new CpInfo?[1],
            AccessFlags = Constants.Flags.ClassAccessFlags.Public,
            ThisClass = new TUShort(0),
            SuperClass = new TUShort(0)
        };
        return new ClassBuilder(classFile);
    }

    public FieldEntry AddField(string name, string descriptor, Constants.Flags.FieldAccessFlags accessFlags)
    {
        var info = new FieldInfo
        {
            AccessFlags = accessFlags,
            NameIndex = new TUShort(0),
            DescriptorIndex = new TUShort(0),
            AttributesCount = new TUShort(0),
            Attributes = Array.Empty<AttributeInfo>()
        };
        var entry = new FieldEntry(info, name, descriptor);
        _fields.Add(entry);
        return entry;
    }

    public void RemoveField(FieldEntry field) => _fields.Remove(field);

    public MethodEntry AddMethod(string name, string descriptor, Constants.Flags.MethodAccessFlags accessFlags)
    {
        var info = new MethodInfo
        {
            AccessFlags = accessFlags,
            NameIndex = new TUShort(0),
            DescriptorIndex = new TUShort(0),
            AttributesCount = new TUShort(0),
            Attributes = Array.Empty<AttributeInfo>()
        };
        var entry = new MethodEntry(info, name, descriptor, null);
        _methods.Add(entry);
        return entry;
    }

    public void RemoveMethod(MethodEntry method) => _methods.Remove(method);

    public static ClassBuilder Read(Stream stream)
    {
        var classFile = ClassFile.Read(stream);
        var builder = new ClassBuilder(classFile);

        foreach (var field in classFile.Fields)
        {
            var (name, descriptor) = ResolveMethodNameAndDescriptor(
                classFile.ConstantPool, field.NameIndex.Value, field.DescriptorIndex.Value);
            builder._fields.Add(new FieldEntry(field, name, descriptor));
        }

        foreach (var method in classFile.Methods)
        {
            var (name, descriptor) = ResolveMethodNameAndDescriptor(
                classFile.ConstantPool, method.NameIndex.Value, method.DescriptorIndex.Value);

            var isStatic = (method.AccessFlags & Constants.Flags.MethodAccessFlags.Static) != 0;
            MethodBody? body = null;

            foreach (var attr in method.Attributes)
            {
                var resolved = attr.ResolveBody(classFile.ConstantPool);
                if (resolved is CodeAttribute codeAttr)
                {
                    body = MethodBody.FromCodeAttribute(codeAttr, classFile.ConstantPool);
                    body.MethodDescriptor = descriptor;
                    body.IsStatic = isStatic;
                    break;
                }
            }

            builder._methods.Add(new MethodEntry(method, name, descriptor, body));
        }

        return builder;
    }

    public void Write(Stream stream)
    {
        var cp = new ConstantPoolBuilder();
        var oldToNew = ImportConstantPool(ClassFile.ConstantPool, cp);

        var newConstantPool = cp.Build();

        ClassFile.ConstantPool = newConstantPool;
        ClassFile.ConstantPoolCount = new TUShort((ushort)newConstantPool.Length);

        if (ClassFile.ThisClass.Value > 0)
        {
            ClassFile.ThisClass = new TUShort((ushort)oldToNew[ClassFile.ThisClass.Value]);
        }

        if (ClassFile.SuperClass.Value > 0)
        {
            ClassFile.SuperClass = new TUShort((ushort)oldToNew[ClassFile.SuperClass.Value]);
        }

        for (var i = 0; i < ClassFile.Interfaces.Length; i++)
        {
            if (ClassFile.Interfaces[i].Value > 0)
            {
                ClassFile.Interfaces[i] = new TUShort((ushort)oldToNew[ClassFile.Interfaces[i].Value]);
            }
        }

        ClassFile.Fields = _fields.Select(f => f.Info).ToArray();
        ClassFile.FieldsCount = new TUShort((ushort)ClassFile.Fields.Length);

        ClassFile.Methods = _methods.Select(m => m.Info).ToArray();
        ClassFile.MethodsCount = new TUShort((ushort)ClassFile.Methods.Length);

        foreach (var entry in _fields)
        {
            RemapField(entry, cp, oldToNew);
        }

        foreach (var entry in _methods)
        {
            RemapMethod(entry, cp, oldToNew);
        }

        ClassFile.Attributes = RemapAttributes(ClassFile.Attributes, oldToNew);

        ClassFile.Write(stream);
    }

    private static void RemapMethod(MethodEntry entry, ConstantPoolBuilder cp, Dictionary<int, int> oldToNew)
    {
        entry.Info.NameIndex = new TUShort((ushort)cp.AddUtf8(entry.Name));
        entry.Info.DescriptorIndex = new TUShort((ushort)cp.AddUtf8(entry.Descriptor));

        if (entry.Body != null)
        {
            var codeAttr = entry.Body.ToCodeAttribute(cp);
            var bodyAttr = AttributeInfo.CreateFromAttribute("Code", codeAttr, cp);
            entry.Info.Attributes = [bodyAttr];
        }
        else
        {
            entry.Info.Attributes = RemapAttributes(entry.Info.Attributes, oldToNew);
        }
        entry.Info.AttributesCount = new TUShort((ushort)entry.Info.Attributes.Length);
    }

    private static void RemapField(FieldEntry entry, ConstantPoolBuilder cp, Dictionary<int, int> oldToNew)
    {
        entry.Info.NameIndex = new TUShort((ushort)cp.AddUtf8(entry.Name));
        entry.Info.DescriptorIndex = new TUShort((ushort)cp.AddUtf8(entry.Descriptor));
        entry.Info.Attributes = RemapAttributes(entry.Info.Attributes, oldToNew);
        entry.Info.AttributesCount = new TUShort((ushort)entry.Info.Attributes.Length);
    }

    private static AttributeInfo[] RemapAttributes(AttributeInfo[] attributes, Dictionary<int, int> oldToNew)
    {
        var result = new AttributeInfo[attributes.Length];
        for (var i = 0; i < attributes.Length; i++)
        {
            var attr = attributes[i];
            if (attr.AttributeNameIndex.Value > 0)
            {
                attr.AttributeNameIndex = new TUShort((ushort)oldToNew[attr.AttributeNameIndex.Value]);
            }

            result[i] = attr;
        }

        return result;
    }

    private static Dictionary<int, int> ImportConstantPool(CpInfo?[] oldPool, ConstantPoolBuilder cp)
    {
        var oldToNew = new Dictionary<int, int> { [0] = 0 };

        for (var i = 1; i < oldPool.Length; i++)
        {
            var entry = oldPool[i];
            if (entry == null)
            {
                continue;
            }

            var newIndex = ImportCpEntry(entry, oldPool, cp);
            oldToNew[i] = newIndex;

            if (entry.Tag == Constants.ConstantPoolTag.Long || entry.Tag == Constants.ConstantPoolTag.Double)
            {
                oldToNew[i + 1] = newIndex + 1;
                i++;
            }
        }

        return oldToNew;
    }

    private static int ImportCpEntry(CpInfo entry, CpInfo?[] pool, ConstantPoolBuilder cp)
    {
        switch (entry)
        {
            case CpUtf8 utf8:
                return cp.AddUtf8(utf8.Value);

            case CpInteger intEntry:
                return cp.AddInteger(intEntry.Bytes.Value);

            case CpFloat floatEntry:
                return cp.AddFloat(floatEntry.Bytes.Value);

            case CpLong longEntry:
                return cp.AddLong(longEntry.Bytes.Value);

            case CpDouble doubleEntry:
                return cp.AddDouble(doubleEntry.Bytes.Value);

            case CpClass classEntry:
            {
                var name = ResolveUtf8(pool, classEntry.NameIndex.Value);
                return cp.AddClass(name);
            }

            case CpString stringEntry:
            {
                var value = ResolveUtf8(pool, stringEntry.StringIndex.Value);
                return cp.AddString(value);
            }

            case CpFieldRef fieldRef:
            {
                var owner = ResolveClassName(pool, fieldRef.ClassIndex.Value);
                var (name, desc) = ResolveNat(pool, fieldRef.NameAndTypeIndex.Value);
                return cp.AddFieldRef(owner, name, desc);
            }

            case CpMethodRef methodRef:
            {
                var owner = ResolveClassName(pool, methodRef.ClassIndex.Value);
                var (name, desc) = ResolveNat(pool, methodRef.NameAndTypeIndex.Value);
                return cp.AddMethodRef(owner, name, desc);
            }

            case CpInterfaceMethodRef imethodRef:
            {
                var owner = ResolveClassName(pool, imethodRef.ClassIndex.Value);
                var (name, desc) = ResolveNat(pool, imethodRef.NameAndTypeIndex.Value);
                return cp.AddInterfaceMethodRef(owner, name, desc);
            }

            case CpNameAndType nat:
            {
                var name = ResolveUtf8(pool, nat.NameIndex.Value);
                var desc = ResolveUtf8(pool, nat.DescriptorIndex.Value);
                return cp.AddNameAndType(name, desc);
            }

            case CpMethodType mt:
            {
                var desc = ResolveUtf8(pool, mt.DescriptorIndex.Value);
                return cp.AddMethodType(desc);
            }

            case CpMethodHandle mh:
                return cp.AddMethodHandle(mh.ReferenceKind.Value, mh.ReferenceIndex.Value);

            case CpDynamic dyn:
            {
                var (name, desc) = ResolveNat(pool, dyn.NameAndTypeIndex.Value);
                return cp.AddDynamic(dyn.BootstrapMethodAttrIndex.Value, name, desc);
            }

            case CpInvokeDynamic indy:
            {
                var (name, desc) = ResolveNat(pool, indy.NameAndTypeIndex.Value);
                return cp.AddInvokeDynamic(indy.BootstrapMethodAttrIndex.Value, name, desc);
            }

            case CpModule mod:
            {
                var name = ResolveUtf8(pool, mod.NameIndex.Value);
                return cp.AddModule(name);
            }

            case CpPackage pkg:
            {
                var name = ResolveUtf8(pool, pkg.NameIndex.Value);
                return cp.AddPackage(name);
            }

            default:
                throw new NotSupportedException($"Unsupported constant pool entry type: {entry.GetType().Name}");
        }
    }

    private static string ResolveUtf8(CpInfo?[] pool, int index)
    {
        return ((CpUtf8)pool[index]!).Value;
    }

    private static string ResolveClassName(CpInfo?[] pool, int classIndex)
    {
        var classEntry = (CpClass)pool[classIndex]!;
        return ResolveUtf8(pool, classEntry.NameIndex.Value);
    }

    private static (string Name, string Descriptor) ResolveNat(CpInfo?[] pool, int natIndex)
    {
        var nat = (CpNameAndType)pool[natIndex]!;
        return (ResolveUtf8(pool, nat.NameIndex.Value), ResolveUtf8(pool, nat.DescriptorIndex.Value));
    }

    private static (string Name, string Descriptor) ResolveMethodNameAndDescriptor(
        CpInfo?[] pool, int nameIndex, int descriptorIndex)
    {
        var name = ResolveUtf8(pool, nameIndex);
        var descriptor = ResolveUtf8(pool, descriptorIndex);
        return (name, descriptor);
    }
}
