namespace Anvil.Instructions;

public enum OperandType
{
    None,
    LocalIndex,           // 1 or 2 bytes (wide)
    ConstantPoolIndex,    // 1 or 2 bytes
    ByteImmediate,
    ShortImmediate,
    BranchOffset,         // 2 or 4 bytes
    IincPair,             // index + value
    InvokeInterfaceArgs,  // index(2) + count(1) + zero(1)
    MultiANewArrayArgs,   // index(2) + dims(1)
    NewArrayAtype,        // 1 byte atype
    TableSwitchData,
    LookupSwitchData,
    InvokeDynamicArgs,    // index(2) + 0(1) + 0(1)
}