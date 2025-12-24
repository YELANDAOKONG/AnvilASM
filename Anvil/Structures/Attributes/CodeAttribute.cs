using Anvil.Interfaces;
using Anvil.Structures.Attributes.Code;
using Anvil.Types;

namespace Anvil.Structures.Attributes;

/// <summary>
/// Represents the Code attribute (§4.7.3).
/// </summary>
public class CodeAttribute : IAttribute
{
    public TUShort MaxStack { get; set; }
    public TUShort MaxLocals { get; set; }
    public byte[] Code { get; set; }
    public ExceptionTableEntry[] ExceptionTable { get; set; }
    public AttributeInfo[] Attributes { get; set; }

    public CodeAttribute()
    {
        Code = Array.Empty<byte>();
        ExceptionTable = Array.Empty<ExceptionTableEntry>();
        Attributes = Array.Empty<AttributeInfo>();
    }

    public void Write(Stream stream)
    {
        MaxStack.Write(stream);
        MaxLocals.Write(stream);
        
        new TUInt((uint)Code.Length).Write(stream);
        stream.Write(Code);

        new TUShort((ushort)ExceptionTable.Length).Write(stream);
        foreach (var entry in ExceptionTable)
        {
            entry.Write(stream);
        }

        new TUShort((ushort)Attributes.Length).Write(stream);
        foreach (var attr in Attributes)
        {
            attr.Write(stream);
        }
    }

    public static CodeAttribute Read(Stream stream)
    {
        var attr = new CodeAttribute();
        
        attr.MaxStack = TUShort.Read(stream);
        attr.MaxLocals = TUShort.Read(stream);

        var codeLength = TUInt.Read(stream).Value;
        attr.Code = new byte[codeLength];
        stream.ReadExactly(attr.Code);

        var exceptionTableLength = TUShort.Read(stream).Value;
        attr.ExceptionTable = new ExceptionTableEntry[exceptionTableLength];
        for (int i = 0; i < exceptionTableLength; i++)
        {
            attr.ExceptionTable[i] = ExceptionTableEntry.Read(stream);
        }

        var attributesCount = TUShort.Read(stream).Value;
        attr.Attributes = new AttributeInfo[attributesCount];
        for (int i = 0; i < attributesCount; i++)
        {
            attr.Attributes[i] = AttributeInfo.Read(stream);
        }

        return attr;
    }
}
