namespace Anvil.Constants.Flags;

[Flags]
public enum ModuleFlags : ushort
{
    Open = 0x0020,
    Synthetic = 0x1000,
    Mandated = 0x8000
}

[Flags]
public enum ModuleRequiresFlags : ushort
{
    Transitive = 0x0020,
    StaticPhase = 0x0040,
    Synthetic = 0x1000,
    Mandated = 0x8000
}

[Flags]
public enum ModuleExportsFlags : ushort
{
    Synthetic = 0x1000,
    Mandated = 0x8000
}

[Flags]
public enum ModuleOpensFlags : ushort
{
    Synthetic = 0x1000,
    Mandated = 0x8000
}