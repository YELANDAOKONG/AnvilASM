using Anvil.Structures.Attributes.StackMap;

namespace Anvil.Structures.Attributes.StackMap.Types;

// Simple singleton-like types that only have a tag.

public class TopVariableInfo : VerificationTypeInfo { public override byte Tag => 0; }
public class IntegerVariableInfo : VerificationTypeInfo { public override byte Tag => 1; }
public class FloatVariableInfo : VerificationTypeInfo { public override byte Tag => 2; }
public class DoubleVariableInfo : VerificationTypeInfo { public override byte Tag => 3; }
public class LongVariableInfo : VerificationTypeInfo { public override byte Tag => 4; }
public class NullVariableInfo : VerificationTypeInfo { public override byte Tag => 5; }
public class UninitializedThisVariableInfo : VerificationTypeInfo { public override byte Tag => 6; }