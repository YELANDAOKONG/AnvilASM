using Anvil.Constants.Flags;
using Anvil.Instructions;

namespace Anvil.Models;

public class MethodNode
{
    public MethodAccessFlags AccessFlags { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Descriptor { get; set; } = string.Empty;
    public MethodBody? Body { get; set; }

    public MethodNode() { }

    public MethodNode(MethodAccessFlags accessFlags, string name, string descriptor, MethodBody? body = null)
    {
        AccessFlags = accessFlags;
        Name = name;
        Descriptor = descriptor;
        Body = body;
    }
}
