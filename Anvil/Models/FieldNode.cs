using Anvil.Constants.Flags;

namespace Anvil.Models;

public class FieldNode
{
    public FieldAccessFlags AccessFlags { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Descriptor { get; set; } = string.Empty;
    public string? Signature { get; set; }
    public List<Anvil.Structures.AttributeInfo> Attributes { get; set; } = [];

    public FieldNode() { }

    public FieldNode(FieldAccessFlags accessFlags, string name, string descriptor)
    {
        AccessFlags = accessFlags;
        Name = name;
        Descriptor = descriptor;
    }
}
