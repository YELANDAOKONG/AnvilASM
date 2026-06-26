using Anvil.Constants;
using Anvil.Structures;
using Anvil.Structures.ConstantPool;
using Anvil.Types;

namespace Anvil.Instructions;

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
        var entry = new CpMethodHandle(
            new TUByte((byte)referenceKind),
            new TUShort((ushort)referenceIndex));
        return Append(entry);
    }

    public int AddInvokeDynamic(int bootstrapMethodAttrIndex, string name, string descriptor)
    {
        var nameAndTypeIndex = AddNameAndType(name, descriptor);
        var entry = new CpInvokeDynamic(
            new TUShort((ushort)bootstrapMethodAttrIndex),
            new TUShort((ushort)nameAndTypeIndex));
        return Append(entry);
    }

    private int Append(CpInfo entry)
    {
        var index = _entries.Count;
        _entries.Add(entry);
        return index;
    }
}
