using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes.Code;

/// <summary>
/// Represents an entry in the exception_table of the Code attribute (§4.7.3).
/// </summary>
public class ExceptionTableEntry : IStructure<ExceptionTableEntry>
{
    public TUShort StartPc { get; set; }
    public TUShort EndPc { get; set; }
    public TUShort HandlerPc { get; set; }
    public TUShort CatchType { get; set; }

    public void Write(Stream stream)
    {
        StartPc.Write(stream);
        EndPc.Write(stream);
        HandlerPc.Write(stream);
        CatchType.Write(stream);
    }

    public static ExceptionTableEntry Read(Stream stream)
    {
        return new ExceptionTableEntry
        {
            StartPc = TUShort.Read(stream),
            EndPc = TUShort.Read(stream),
            HandlerPc = TUShort.Read(stream),
            CatchType = TUShort.Read(stream)
        };
    }
}