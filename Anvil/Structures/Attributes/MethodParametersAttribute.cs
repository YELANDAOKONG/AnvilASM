using Anvil.Constants.Flags;
using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes;

public class MethodParametersAttribute : IStructure<MethodParametersAttribute>, IAttribute
{
    public TUByte ParametersCount { get; set; }
    public MethodParameterEntry[] Parameters { get; set; } = Array.Empty<MethodParameterEntry>();

    public void Write(Stream stream)
    {
        ParametersCount.Write(stream);
        foreach (var param in Parameters) param.Write(stream);
    }

    public static MethodParametersAttribute Read(Stream stream)
    {
        var attr = new MethodParametersAttribute();
        attr.ParametersCount = TUByte.Read(stream);
        attr.Parameters = new MethodParameterEntry[attr.ParametersCount.Value];
        for (int i = 0; i < attr.Parameters.Length; i++)
        {
            attr.Parameters[i] = MethodParameterEntry.Read(stream);
        }
        return attr;
    }
}

public class MethodParameterEntry : IStructure<MethodParameterEntry>
{
    public TUShort NameIndex { get; set; }
    public ParameterAccessFlags AccessFlags { get; set; }

    public void Write(Stream stream)
    {
        NameIndex.Write(stream);
        new TUShort((ushort)AccessFlags).Write(stream);
    }

    public static MethodParameterEntry Read(Stream stream)
    {
        return new MethodParameterEntry
        {
            NameIndex = TUShort.Read(stream),
            AccessFlags = (ParameterAccessFlags)TUShort.Read(stream).Value
        };
    }
}