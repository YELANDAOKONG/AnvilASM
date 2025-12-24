using Anvil.Constants;
using Anvil.Types;

namespace Anvil.Structures.ConstantPool;

// §4.4.4 CONSTANT_Integer_info
public class CpInteger : CpInfo
{
    public override ConstantPoolTag Tag => ConstantPoolTag.Integer;
    public TInt Bytes { get; set; } // u4 bytes
    
    public CpInteger(TInt bytes) => Bytes = bytes;
    protected override void WriteInfo(Stream stream) => Bytes.Write(stream);
    internal static CpInteger ReadInfo(Stream stream) => new(TInt.Read(stream));
}

// §4.4.4 CONSTANT_Float_info
public class CpFloat : CpInfo
{
    public override ConstantPoolTag Tag => ConstantPoolTag.Float;
    public TFloat Bytes { get; set; } // u4 bytes
    
    public CpFloat(TFloat bytes) => Bytes = bytes;
    protected override void WriteInfo(Stream stream) => Bytes.Write(stream);
    internal static CpFloat ReadInfo(Stream stream) => new(TFloat.Read(stream));
}

// §4.4.5 CONSTANT_Long_info
public class CpLong : CpInfo
{
    public override ConstantPoolTag Tag => ConstantPoolTag.Long;
    // JVM Spec defines high_bytes (u4) and low_bytes (u4). 
    // TLong handles 8 bytes big-endian read/write, which is binary equivalent.
    public TLong Bytes { get; set; } 

    public CpLong(TLong bytes) => Bytes = bytes;
    protected override void WriteInfo(Stream stream) => Bytes.Write(stream);
    internal static CpLong ReadInfo(Stream stream) => new(TLong.Read(stream));
}

// §4.4.5 CONSTANT_Double_info
public class CpDouble : CpInfo
{
    public override ConstantPoolTag Tag => ConstantPoolTag.Double;
    public TDouble Bytes { get; set; }

    public CpDouble(TDouble bytes) => Bytes = bytes;
    protected override void WriteInfo(Stream stream) => Bytes.Write(stream);
    internal static CpDouble ReadInfo(Stream stream) => new(TDouble.Read(stream));
}