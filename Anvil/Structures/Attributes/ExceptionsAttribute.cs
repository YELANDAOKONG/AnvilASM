using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes;

/// <summary>
/// Represents the Exceptions attribute (§4.7.5).
/// </summary>
public class ExceptionsAttribute : IAttribute
{
    public TUShort[] ExceptionIndexTable { get; set; }

    public ExceptionsAttribute(TUShort[] exceptionIndexTable)
    {
        ExceptionIndexTable = exceptionIndexTable;
    }

    public void Write(Stream stream)
    {
        new TUShort((ushort)ExceptionIndexTable.Length).Write(stream);
        foreach (var index in ExceptionIndexTable)
        {
            index.Write(stream);
        }
    }

    public static ExceptionsAttribute Read(Stream stream)
    {
        var count = TUShort.Read(stream).Value;
        var table = new TUShort[count];
        
        for (int i = 0; i < count; i++)
        {
            table[i] = TUShort.Read(stream);
        }

        return new ExceptionsAttribute(table);
    }
}