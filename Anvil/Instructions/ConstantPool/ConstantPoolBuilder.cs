using Anvil.Constants;
using Anvil.Structures;
using Anvil.Structures.ConstantPool;
using Anvil.Types;

namespace Anvil.Instructions.ConstantPool;

public class ConstantPoolBuilder
{
    private readonly List<CpInfo> _entries = new() { null! };

    private readonly Dictionary<string, int> _utf8Map = new();
    private readonly Dictionary<string, int> _classMap = new();
    private readonly Dictionary<string, int> _stringMap = new();
    private readonly Dictionary<int, int> _integerMap = new();
    private readonly Dictionary<int, int> _floatMap = new();
    private readonly Dictionary<long, int> _longMap = new();
    private readonly Dictionary<long, int> _doubleMap = new();
    private readonly Dictionary<(string Name, string Descriptor), int> _nameAndTypeMap = new();
    private readonly Dictionary<(string Owner, string Name, string Descriptor), int> _fieldRefMap = new();
    private readonly Dictionary<(string Owner, string Name, string Descriptor), int> _methodRefMap = new();
    private readonly Dictionary<(string Owner, string Name, string Descriptor), int> _interfaceMethodRefMap = new();
    private readonly Dictionary<string, int> _methodTypeMap = new();
    private readonly Dictionary<(int ReferenceKind, int ReferenceIndex), int> _methodHandleMap = new();
    private readonly Dictionary<(int BootstrapIndex, string Name, string Descriptor), int> _dynamicMap = new();
    private readonly Dictionary<(int BootstrapIndex, string Name, string Descriptor), int> _invokeDynamicMap = new();
    private readonly Dictionary<string, int> _moduleMap = new();
    private readonly Dictionary<string, int> _packageMap = new();

    public ConstantPoolBuilder()
    {
    }

    public ConstantPoolBuilder(CpInfo?[] existingPool)
    {
        ArgumentNullException.ThrowIfNull(existingPool);
        if (existingPool.Length == 0 || existingPool[0] is not null)
        {
            throw new ArgumentException(
                "A JVM constant pool must contain a null entry at index zero.",
                nameof(existingPool));
        }

        _entries.Clear();
        _entries.AddRange(existingPool.Select(entry => entry!));
        IndexExistingEntries(existingPool);
    }

    public int Count => _entries.Count;

    public CpInfo[] Build()
    {
        var result = new CpInfo[_entries.Count];
        for (var i = 0; i < _entries.Count; i++)
        {
            result[i] = _entries[i];
        }

        return result;
    }

    public int AddUtf8(string value)
    {
        if (_utf8Map.TryGetValue(value, out var index))
        {
            return index;
        }

        var entry = new CpUtf8(value);
        index = Append(entry);
        _utf8Map[value] = index;
        return index;
    }

    public int AddClass(string name)
    {
        if (_classMap.TryGetValue(name, out var index))
        {
            return index;
        }

        var nameIndex = AddUtf8(name);
        var entry = new CpClass(new TUShort((ushort)nameIndex));
        index = Append(entry);
        _classMap[name] = index;
        return index;
    }

    public int AddString(string value)
    {
        if (_stringMap.TryGetValue(value, out var index))
        {
            return index;
        }

        var utf8Index = AddUtf8(value);
        var entry = new CpString(new TUShort((ushort)utf8Index));
        index = Append(entry);
        _stringMap[value] = index;
        return index;
    }

    public int AddInteger(int value)
    {
        if (_integerMap.TryGetValue(value, out var index))
        {
            return index;
        }

        var entry = new CpInteger(new TInt(value));
        index = Append(entry);
        _integerMap[value] = index;
        return index;
    }

    public int AddFloat(float value)
    {
        var bits = BitConverter.SingleToInt32Bits(value);
        if (_floatMap.TryGetValue(bits, out var index))
        {
            return index;
        }

        var entry = new CpFloat(new TFloat(value));
        index = Append(entry);
        _floatMap[bits] = index;
        return index;
    }

    public int AddLong(long value)
    {
        if (_longMap.TryGetValue(value, out var index))
        {
            return index;
        }

        var entry = new CpLong(new TLong(value));
        index = Append(entry);
        _entries.Add(null!);
        _longMap[value] = index;
        return index;
    }

    public int AddDouble(double value)
    {
        var bits = BitConverter.DoubleToInt64Bits(value);
        if (_doubleMap.TryGetValue(bits, out var index))
        {
            return index;
        }

        var entry = new CpDouble(new TDouble(value));
        index = Append(entry);
        _entries.Add(null!);
        _doubleMap[bits] = index;
        return index;
    }

    public int AddNameAndType(string name, string descriptor)
    {
        var key = (name, descriptor);
        if (_nameAndTypeMap.TryGetValue(key, out var index))
        {
            return index;
        }

        var nameIndex = AddUtf8(name);
        var descriptorIndex = AddUtf8(descriptor);
        var entry = new CpNameAndType(
            new TUShort((ushort)nameIndex),
            new TUShort((ushort)descriptorIndex));
        index = Append(entry);
        _nameAndTypeMap[key] = index;
        return index;
    }

    public int AddFieldRef(string owner, string name, string descriptor)
    {
        var key = (owner, name, descriptor);
        if (_fieldRefMap.TryGetValue(key, out var index))
        {
            return index;
        }

        var classIndex = AddClass(owner);
        var nameAndTypeIndex = AddNameAndType(name, descriptor);
        var entry = new CpFieldRef(
            new TUShort((ushort)classIndex),
            new TUShort((ushort)nameAndTypeIndex));
        index = Append(entry);
        _fieldRefMap[key] = index;
        return index;
    }

    public int AddMethodRef(string owner, string name, string descriptor)
    {
        var key = (owner, name, descriptor);
        if (_methodRefMap.TryGetValue(key, out var index))
        {
            return index;
        }

        var classIndex = AddClass(owner);
        var nameAndTypeIndex = AddNameAndType(name, descriptor);
        var entry = new CpMethodRef(
            new TUShort((ushort)classIndex),
            new TUShort((ushort)nameAndTypeIndex));
        index = Append(entry);
        _methodRefMap[key] = index;
        return index;
    }

    public int AddInterfaceMethodRef(string owner, string name, string descriptor)
    {
        var key = (owner, name, descriptor);
        if (_interfaceMethodRefMap.TryGetValue(key, out var index))
        {
            return index;
        }

        var classIndex = AddClass(owner);
        var nameAndTypeIndex = AddNameAndType(name, descriptor);
        var entry = new CpInterfaceMethodRef(
            new TUShort((ushort)classIndex),
            new TUShort((ushort)nameAndTypeIndex));
        index = Append(entry);
        _interfaceMethodRefMap[key] = index;
        return index;
    }

    public int AddMethodType(string descriptor)
    {
        if (_methodTypeMap.TryGetValue(descriptor, out var index))
        {
            return index;
        }

        var descriptorIndex = AddUtf8(descriptor);
        var entry = new CpMethodType(new TUShort((ushort)descriptorIndex));
        index = Append(entry);
        _methodTypeMap[descriptor] = index;
        return index;
    }

    public int AddMethodHandle(int referenceKind, int referenceIndex)
    {
        var key = (referenceKind, referenceIndex);
        if (_methodHandleMap.TryGetValue(key, out var index))
        {
            return index;
        }

        var entry = new CpMethodHandle(
            new TUByte((byte)referenceKind),
            new TUShort((ushort)referenceIndex));
        index = Append(entry);
        _methodHandleMap[key] = index;
        return index;
    }

    public int AddInvokeDynamic(int bootstrapMethodAttrIndex, string name, string descriptor)
    {
        var key = (bootstrapMethodAttrIndex, name, descriptor);
        if (_invokeDynamicMap.TryGetValue(key, out var index))
        {
            return index;
        }

        var nameAndTypeIndex = AddNameAndType(name, descriptor);
        var entry = new CpInvokeDynamic(
            new TUShort((ushort)bootstrapMethodAttrIndex),
            new TUShort((ushort)nameAndTypeIndex));
        index = Append(entry);
        _invokeDynamicMap[key] = index;
        return index;
    }

    public int AddDynamic(int bootstrapMethodAttrIndex, string name, string descriptor)
    {
        var key = (bootstrapMethodAttrIndex, name, descriptor);
        if (_dynamicMap.TryGetValue(key, out var index))
        {
            return index;
        }

        var nameAndTypeIndex = AddNameAndType(name, descriptor);
        var entry = new CpDynamic(
            new TUShort((ushort)bootstrapMethodAttrIndex),
            new TUShort((ushort)nameAndTypeIndex));
        index = Append(entry);
        _dynamicMap[key] = index;
        return index;
    }

    public int AddModule(string name)
    {
        if (_moduleMap.TryGetValue(name, out var index))
        {
            return index;
        }

        var nameIndex = AddUtf8(name);
        var entry = new CpModule(new TUShort((ushort)nameIndex));
        index = Append(entry);
        _moduleMap[name] = index;
        return index;
    }

    public int AddPackage(string name)
    {
        if (_packageMap.TryGetValue(name, out var index))
        {
            return index;
        }

        var nameIndex = AddUtf8(name);
        var entry = new CpPackage(new TUShort((ushort)nameIndex));
        index = Append(entry);
        _packageMap[name] = index;
        return index;
    }

    private int Append(CpInfo entry)
    {
        var index = _entries.Count;
        _entries.Add(entry);
        return index;
    }

    private void IndexExistingEntries(CpInfo?[] constantPool)
    {
        for (var index = 1; index < constantPool.Length; index++)
        {
            switch (constantPool[index])
            {
                case CpUtf8 utf8:
                    _utf8Map.TryAdd(utf8.Value, index);
                    break;
                case CpClass classEntry:
                    _classMap.TryAdd(
                        ResolveUtf8(constantPool, classEntry.NameIndex.Value),
                        index);
                    break;
                case CpString stringEntry:
                    _stringMap.TryAdd(
                        ResolveUtf8(constantPool, stringEntry.StringIndex.Value),
                        index);
                    break;
                case CpInteger integerEntry:
                    _integerMap.TryAdd(integerEntry.Bytes.Value, index);
                    break;
                case CpFloat floatEntry:
                    _floatMap.TryAdd(
                        BitConverter.SingleToInt32Bits(floatEntry.Bytes.Value),
                        index);
                    break;
                case CpLong longEntry:
                    _longMap.TryAdd(longEntry.Bytes.Value, index);
                    break;
                case CpDouble doubleEntry:
                    _doubleMap.TryAdd(
                        BitConverter.DoubleToInt64Bits(doubleEntry.Bytes.Value),
                        index);
                    break;
                case CpNameAndType nameAndType:
                    _nameAndTypeMap.TryAdd(
                        (
                            ResolveUtf8(constantPool, nameAndType.NameIndex.Value),
                            ResolveUtf8(constantPool, nameAndType.DescriptorIndex.Value)
                        ),
                        index);
                    break;
                case CpFieldRef fieldReference:
                    _fieldRefMap.TryAdd(
                        ResolveMemberKey(
                            constantPool,
                            fieldReference.ClassIndex.Value,
                            fieldReference.NameAndTypeIndex.Value),
                        index);
                    break;
                case CpMethodRef methodReference:
                    _methodRefMap.TryAdd(
                        ResolveMemberKey(
                            constantPool,
                            methodReference.ClassIndex.Value,
                            methodReference.NameAndTypeIndex.Value),
                        index);
                    break;
                case CpInterfaceMethodRef interfaceMethodReference:
                    _interfaceMethodRefMap.TryAdd(
                        ResolveMemberKey(
                            constantPool,
                            interfaceMethodReference.ClassIndex.Value,
                            interfaceMethodReference.NameAndTypeIndex.Value),
                        index);
                    break;
                case CpMethodType methodType:
                    _methodTypeMap.TryAdd(
                        ResolveUtf8(constantPool, methodType.DescriptorIndex.Value),
                        index);
                    break;
                case CpMethodHandle methodHandle:
                    _methodHandleMap.TryAdd(
                        (methodHandle.ReferenceKind.Value, methodHandle.ReferenceIndex.Value),
                        index);
                    break;
                case CpDynamic dynamic:
                {
                    var (name, descriptor) = ResolveNameAndType(
                        constantPool,
                        dynamic.NameAndTypeIndex.Value);
                    _dynamicMap.TryAdd(
                        (dynamic.BootstrapMethodAttrIndex.Value, name, descriptor),
                        index);
                    break;
                }
                case CpInvokeDynamic invokeDynamic:
                {
                    var (name, descriptor) = ResolveNameAndType(
                        constantPool,
                        invokeDynamic.NameAndTypeIndex.Value);
                    _invokeDynamicMap.TryAdd(
                        (invokeDynamic.BootstrapMethodAttrIndex.Value, name, descriptor),
                        index);
                    break;
                }
                case CpModule module:
                    _moduleMap.TryAdd(
                        ResolveUtf8(constantPool, module.NameIndex.Value),
                        index);
                    break;
                case CpPackage package:
                    _packageMap.TryAdd(
                        ResolveUtf8(constantPool, package.NameIndex.Value),
                        index);
                    break;
            }
        }
    }

    private static string ResolveUtf8(CpInfo?[] constantPool, int index)
    {
        return constantPool[index] is CpUtf8 utf8
            ? utf8.Value
            : throw new FormatException(
                $"Constant pool entry {index} must be a CONSTANT_Utf8_info.");
    }

    private static (string Owner, string Name, string Descriptor) ResolveMemberKey(
        CpInfo?[] constantPool,
        int classIndex,
        int nameAndTypeIndex)
    {
        var classEntry = constantPool[classIndex] as CpClass
            ?? throw new FormatException(
                $"Constant pool entry {classIndex} must be a CONSTANT_Class_info.");
        var nameAndType = constantPool[nameAndTypeIndex] as CpNameAndType
            ?? throw new FormatException(
                $"Constant pool entry {nameAndTypeIndex} must be a CONSTANT_NameAndType_info.");

        return (
            ResolveUtf8(constantPool, classEntry.NameIndex.Value),
            ResolveUtf8(constantPool, nameAndType.NameIndex.Value),
            ResolveUtf8(constantPool, nameAndType.DescriptorIndex.Value)
        );
    }

    private static (string Name, string Descriptor) ResolveNameAndType(
        CpInfo?[] constantPool,
        int index)
    {
        var entry = constantPool[index] as CpNameAndType
            ?? throw new FormatException(
                $"Constant pool entry {index} must be a CONSTANT_NameAndType_info.");
        return (
            ResolveUtf8(constantPool, entry.NameIndex.Value),
            ResolveUtf8(constantPool, entry.DescriptorIndex.Value)
        );
    }
}
