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
    public string? Signature { get; set; }
    public List<string> Exceptions { get; set; } = [];
    public MethodBody? Body { get; set; }
    public List<AttributeInfo> Attributes { get; set; } = [];

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
    public string? Signature { get; set; }
    public List<AttributeInfo> Attributes { get; set; } = [];

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
    
    public string Name { get; set; } = string.Empty;
    public string SuperName { get; set; } = string.Empty;
    public string? Signature { get; set; }
    public string? SourceFile { get; set; }
    public List<string> Interfaces { get; set; } = [];
    public List<AttributeInfo> Attributes { get; set; } = [];

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

        if (classFile.ThisClass.Value > 0)
        {
            builder.Name = ResolveClassName(classFile.ConstantPool, classFile.ThisClass.Value);
        }
        
        if (classFile.SuperClass.Value > 0)
        {
            builder.SuperName = ResolveClassName(classFile.ConstantPool, classFile.SuperClass.Value);
        }

        foreach (var iface in classFile.Interfaces)
        {
            if (iface.Value > 0)
            {
                builder.Interfaces.Add(ResolveClassName(classFile.ConstantPool, iface.Value));
            }
        }

        foreach (var attr in classFile.Attributes)
        {
            var resolved = attr.ResolveBody(classFile.ConstantPool);
            if (resolved is SignatureAttribute sig)
                builder.Signature = ResolveUtf8(classFile.ConstantPool, sig.SignatureIndex.Value);
            else if (resolved is SourceFileAttribute src)
                builder.SourceFile = ResolveUtf8(classFile.ConstantPool, src.SourceFileIndex.Value);
            else
                builder.Attributes.Add(attr);
        }

        foreach (var field in classFile.Fields)
        {
            var (name, descriptor) = ResolveMethodNameAndDescriptor(
                classFile.ConstantPool, field.NameIndex.Value, field.DescriptorIndex.Value);
            var fEntry = new FieldEntry(field, name, descriptor);

            foreach (var attr in field.Attributes)
            {
                var resolved = attr.ResolveBody(classFile.ConstantPool);
                if (resolved is SignatureAttribute sig)
                    fEntry.Signature = ResolveUtf8(classFile.ConstantPool, sig.SignatureIndex.Value);
                else
                    fEntry.Attributes.Add(attr);
            }
            builder._fields.Add(fEntry);
        }

        foreach (var method in classFile.Methods)
        {
            var (name, descriptor) = ResolveMethodNameAndDescriptor(
                classFile.ConstantPool, method.NameIndex.Value, method.DescriptorIndex.Value);

            var isStatic = (method.AccessFlags & Constants.Flags.MethodAccessFlags.Static) != 0;
            MethodBody? body = null;
            var mEntry = new MethodEntry(method, name, descriptor, null);

            foreach (var attr in method.Attributes)
            {
                var resolved = attr.ResolveBody(classFile.ConstantPool);
                if (resolved is CodeAttribute codeAttr)
                {
                    body = MethodBody.FromCodeAttribute(codeAttr, classFile.ConstantPool);
                    body.MethodDescriptor = descriptor;
                    body.MethodName = name;
                    body.OwnerInternalName = builder.Name;
                    body.IsStatic = isStatic;
                }
                else if (resolved is SignatureAttribute sig)
                {
                    mEntry.Signature = ResolveUtf8(classFile.ConstantPool, sig.SignatureIndex.Value);
                }
                else if (resolved is ExceptionsAttribute exc)
                {
                    foreach (var idx in exc.ExceptionIndexTable)
                        mEntry.Exceptions.Add(ResolveClassName(classFile.ConstantPool, idx.Value));
                }
                else
                {
                    mEntry.Attributes.Add(attr);
                }
            }

            mEntry.Body = body;
            builder._methods.Add(mEntry);
        }

        return builder;
    }

    public void Write(Stream stream)
    {
        var cp = new ConstantPoolBuilder(ClassFile.ConstantPool);
        var oldToNew = Enumerable.Range(0, ClassFile.ConstantPool.Length)
            .ToDictionary(index => index);

        if (!string.IsNullOrEmpty(Name))
        {
            ClassFile.ThisClass = new TUShort((ushort)cp.AddClass(Name));
        }

        if (!string.IsNullOrEmpty(SuperName))
        {
            ClassFile.SuperClass = new TUShort((ushort)cp.AddClass(SuperName));
        }

        var interfaces = new TUShort[Interfaces.Count];
        for (var i = 0; i < Interfaces.Count; i++)
        {
            interfaces[i] = new TUShort((ushort)cp.AddClass(Interfaces[i]));
        }
        ClassFile.Interfaces = interfaces;
        ClassFile.InterfacesCount = new TUShort((ushort)interfaces.Length);

        var classAttrs = new List<AttributeInfo>(RemapAttributes(Attributes.ToArray(), oldToNew));
        if (!string.IsNullOrEmpty(Signature))
            classAttrs.Add(AttributeInfo.CreateFromAttribute("Signature", new SignatureAttribute { SignatureIndex = new TUShort((ushort)cp.AddUtf8(Signature)) }, cp));
        if (!string.IsNullOrEmpty(SourceFile))
            classAttrs.Add(AttributeInfo.CreateFromAttribute("SourceFile", new SourceFileAttribute { SourceFileIndex = new TUShort((ushort)cp.AddUtf8(SourceFile)) }, cp));
        ClassFile.Attributes = classAttrs.ToArray();
        ClassFile.AttributesCount = new TUShort((ushort)ClassFile.Attributes.Length);

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
            RemapMethod(entry, Name, cp, oldToNew);
        }

        var newConstantPool = cp.Build();
        ClassFile.ConstantPool = newConstantPool;
        ClassFile.ConstantPoolCount = new TUShort((ushort)newConstantPool.Length);

        ClassFile.Write(stream);
    }

    private static void RemapMethod(
        MethodEntry entry,
        string ownerInternalName,
        ConstantPoolBuilder cp,
        Dictionary<int, int> oldToNew)
    {
        entry.Info.NameIndex = new TUShort((ushort)cp.AddUtf8(entry.Name));
        entry.Info.DescriptorIndex = new TUShort((ushort)cp.AddUtf8(entry.Descriptor));

        var remappedAttrs = new List<AttributeInfo>(RemapAttributes(entry.Attributes.ToArray(), oldToNew));

        if (!string.IsNullOrEmpty(entry.Signature))
            remappedAttrs.Add(AttributeInfo.CreateFromAttribute("Signature", new SignatureAttribute { SignatureIndex = new TUShort((ushort)cp.AddUtf8(entry.Signature)) }, cp));

        if (entry.Exceptions.Count > 0)
        {
            var idxs = entry.Exceptions.Select(e => new TUShort((ushort)cp.AddClass(e))).ToArray();
            remappedAttrs.Add(AttributeInfo.CreateFromAttribute("Exceptions", new ExceptionsAttribute(idxs), cp));
        }

        if (entry.Body != null)
        {
            entry.Body.MethodName = entry.Name;
            entry.Body.MethodDescriptor = entry.Descriptor;
            entry.Body.OwnerInternalName = ownerInternalName;
            entry.Body.IsStatic =
                (entry.Info.AccessFlags & Constants.Flags.MethodAccessFlags.Static) != 0;
            var codeAttr = entry.Body.ToCodeAttribute(cp);
            var bodyAttr = AttributeInfo.CreateFromAttribute("Code", codeAttr, cp);
            remappedAttrs.Add(bodyAttr);
        }

        entry.Info.Attributes = remappedAttrs.ToArray();
        entry.Info.AttributesCount = new TUShort((ushort)entry.Info.Attributes.Length);
    }

    private static void RemapField(FieldEntry entry, ConstantPoolBuilder cp, Dictionary<int, int> oldToNew)
    {
        entry.Info.NameIndex = new TUShort((ushort)cp.AddUtf8(entry.Name));
        entry.Info.DescriptorIndex = new TUShort((ushort)cp.AddUtf8(entry.Descriptor));

        var remappedAttrs = new List<AttributeInfo>(RemapAttributes(entry.Attributes.ToArray(), oldToNew));
        if (!string.IsNullOrEmpty(entry.Signature))
            remappedAttrs.Add(AttributeInfo.CreateFromAttribute("Signature", new SignatureAttribute { SignatureIndex = new TUShort((ushort)cp.AddUtf8(entry.Signature)) }, cp));

        entry.Info.Attributes = remappedAttrs.ToArray();
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
