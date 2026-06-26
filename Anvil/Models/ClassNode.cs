using Anvil.Constants.Flags;

namespace Anvil.Models;

public class ClassNode
{
    public int MajorVersion { get; set; } = 65;
    public int MinorVersion { get; set; } = 0;
    
    public ClassAccessFlags AccessFlags { get; set; } = ClassAccessFlags.Public;
    
    public string Name { get; set; } = string.Empty;
    public string SuperName { get; set; } = "java/lang/Object";
    
    public List<string> Interfaces { get; set; } = [];
    public List<FieldNode> Fields { get; set; } = [];
    public List<MethodNode> Methods { get; set; } = [];
}
