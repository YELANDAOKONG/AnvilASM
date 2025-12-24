namespace Anvil.Descriptors;

/// <summary>
/// Represents the base type of a field or return value.
/// Spec §4.3.2 Table 4.3-A
/// </summary>
public enum DescriptorTag
{
    Byte,       // B
    Char,       // C
    Double,     // D
    Float,      // F
    Int,        // I
    Long,       // J
    Short,      // S
    Boolean,    // Z
    Void,       // V (ReturnDescriptor only)
    Object,     // L ClassName ;
    Array       // [ ComponentType
}