namespace Anvil.Instructions;

public class Operand
{
    public byte[] Data { get; set; } = [];
    
    public override string ToString()
    {
        return BitConverter.ToString(Data).Replace("-", string.Empty);
    }
    
    public static Operand Empty => new Operand { Data = Array.Empty<byte>() };

}