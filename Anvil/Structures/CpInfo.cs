using Anvil.Constants;
using Anvil.Interfaces;
using Anvil.Structures.ConstantPool;
using Anvil.Types;

namespace Anvil.Structures;

/// <summary>
/// Represents the generic format of a constant pool entry.
/// <br/>
/// Spec §4.4: cp_info { u1 tag; u1 info[]; }
/// </summary>
public abstract class CpInfo : IStructure<CpInfo>
{
    public abstract ConstantPoolTag Tag { get; }

    /// <summary>
    /// Writes the Tag followed by the specific Info structure.
    /// </summary>
    public void Write(Stream stream)
    {
        new TUByte((byte)Tag).Write(stream);
        WriteInfo(stream);
    }

    /// <summary>
    /// Implemented by subclasses to write their specific body (info[]).
    /// </summary>
    protected abstract void WriteInfo(Stream stream);

    /// <summary>
    /// Reads the Tag and dispatches to the correct subclass parser.
    /// </summary>
    public static CpInfo Read(Stream stream)
    {
        var tagByte = TUByte.Read(stream);
        var tag = (ConstantPoolTag)tagByte.Value;

        return tag switch
        {
            ConstantPoolTag.Utf8                => CpUtf8.ReadInfo(stream),
            ConstantPoolTag.Integer             => CpInteger.ReadInfo(stream),
            ConstantPoolTag.Float               => CpFloat.ReadInfo(stream),
            ConstantPoolTag.Long                => CpLong.ReadInfo(stream),
            ConstantPoolTag.Double              => CpDouble.ReadInfo(stream),
            ConstantPoolTag.Class               => CpClass.ReadInfo(stream),
            ConstantPoolTag.String              => CpString.ReadInfo(stream),
            ConstantPoolTag.Fieldref            => CpFieldRef.ReadInfo(stream),
            ConstantPoolTag.Methodref           => CpMethodRef.ReadInfo(stream),
            ConstantPoolTag.InterfaceMethodref  => CpInterfaceMethodRef.ReadInfo(stream),
            ConstantPoolTag.NameAndType         => CpNameAndType.ReadInfo(stream),
            ConstantPoolTag.MethodHandle        => CpMethodHandle.ReadInfo(stream),
            ConstantPoolTag.MethodType          => CpMethodType.ReadInfo(stream),
            ConstantPoolTag.Dynamic             => CpDynamic.ReadInfo(stream),
            ConstantPoolTag.InvokeDynamic       => CpInvokeDynamic.ReadInfo(stream),
            ConstantPoolTag.Module              => CpModule.ReadInfo(stream),
            ConstantPoolTag.Package             => CpPackage.ReadInfo(stream),
            _ => throw new NotSupportedException($"Unknown Constant Pool Tag: {tag} (0x{tagByte.Value:X2})")
        };
    }
}
