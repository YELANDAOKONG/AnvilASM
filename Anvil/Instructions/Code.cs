using System.Text;

namespace Anvil.Instructions;

public class Code
{
    public OperationCode? Prefix { get; set; } = null; // Example: WIDE
    public OperationCode OpCode { get; set; }
    public List<Operand> Operands { get; set; } = new List<Operand>();

    public Code(OperationCode opCode, IEnumerable<Operand>? operands = null)
    {
        OpCode = opCode;
        if (operands != null) Operands.AddRange(operands);
    }
    
    public Code(OperationCode prefix, OperationCode opCode, IEnumerable<Operand>? operands = null)
        : this(opCode, operands)
    {
        Prefix = prefix;
    }
    
    public override string ToString()
    {
        var operands = new StringBuilder();
        foreach (var operand in Operands)
        {
            operands.Append($"0x{operand.ToString()}");
            operands.Append(", ");
        }
        var data = operands.ToString();
        data = data.Trim(' ');
        data = data.TrimEnd(',');
        
        if (Prefix != null)
        {
            return $"({Prefix.ToString()}) {OpCode.ToString()} [{data}]";
        }

        return $"{OpCode.ToString()} [{data}]";
    }
}