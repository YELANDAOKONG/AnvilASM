using Anvil.Constants;
using Anvil.Types;
using Anvil.Utilities;

namespace Anvil.Structures.ConstantPool;

public class CpUtf8 : CpInfo
{
    public override ConstantPoolTag Tag => ConstantPoolTag.Utf8;
    public string Value { get; set; }

    public CpUtf8(string value) => Value = value;

    protected override void WriteInfo(Stream stream)
    {
        var bytes = ModifiedUtf8.Encode(Value);
        new TUShort((ushort)bytes.Length).Write(stream);
        stream.Write(bytes);
    }

    internal static CpUtf8 ReadInfo(Stream stream)
    {
        var length = TUShort.Read(stream);
        var buffer = new byte[length.Value];
        stream.ReadExactly(buffer);
        return new CpUtf8(ModifiedUtf8.Decode(buffer));
    }
}