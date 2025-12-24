using Anvil.Interfaces;
using Anvil.Types;

namespace Anvil.Structures.Attributes;

public class NestHostAttribute : IStructure<NestHostAttribute>, IAttribute
{
    public TUShort HostClassIndex { get; set; }

    public void Write(Stream stream) => HostClassIndex.Write(stream);

    public static NestHostAttribute Read(Stream stream) 
        => new() { HostClassIndex = TUShort.Read(stream) };
}