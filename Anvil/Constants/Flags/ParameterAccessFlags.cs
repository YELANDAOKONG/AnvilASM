namespace Anvil.Constants.Flags;

[Flags]
public enum ParameterAccessFlags : ushort
{
    Final = 0x0010,
    Synthetic = 0x1000,
    Mandated = 0x8000
}