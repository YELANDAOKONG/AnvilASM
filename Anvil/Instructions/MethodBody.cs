using Anvil.Structures;
using Anvil.Structures.Attributes;
using Anvil.Structures.Attributes.Code;
using Anvil.Types;

namespace Anvil.Instructions;

public class MethodBody
{
    public int MaxStack { get; set; }
    public int MaxLocals { get; set; }

    public List<Instruction> Instructions { get; set; } = [];
    public List<TryCatchBlock> TryCatchBlocks { get; set; } = [];
    public List<AttributeInfo> Attributes { get; set; } = [];

    private readonly Dictionary<Label, int> _labelOffsets = new();

    public void ResolveLabels()
    {
        _labelOffsets.Clear();

        var pc = 0;
        foreach (var instruction in Instructions)
        {
            instruction.Offset = pc;
            pc += instruction.GetSize();

            foreach (var label in instruction.Labels)
            {
                label.Offset = instruction.Offset;
                _labelOffsets[label] = instruction.Offset.Value;
            }
        }

        foreach (var instruction in Instructions)
        {
            switch (instruction)
            {
                case JumpInstruction jump:
                {
                    var targetOffset = GetLabelOffset(jump.Target);
                    jump.BranchOffset = targetOffset - jump.Offset!.Value;
                    break;
                }
                case TableSwitchInstruction table:
                {
                    table.DefaultOffset = GetLabelOffset(table.DefaultTarget) - table.Offset!.Value;
                    table.TargetOffsets = table.Targets
                        .Select(t => GetLabelOffset(t) - table.Offset!.Value)
                        .ToList();
                    break;
                }
                case LookupSwitchInstruction lookup:
                {
                    lookup.DefaultOffset = GetLabelOffset(lookup.DefaultTarget) - lookup.Offset!.Value;
                    lookup.ResolvedPairs = lookup.Pairs
                        .Select(p => (p.Key, GetLabelOffset(p.Target) - lookup.Offset!.Value))
                        .ToList();
                    break;
                }
            }
        }
    }

    public void WriteBytecode(Stream stream)
    {
        foreach (var instruction in Instructions)
        {
            instruction.Write(stream);
        }
    }

    public CodeAttribute ToCodeAttribute()
    {
        ResolveLabels();

        using var codeStream = new MemoryStream();
        WriteBytecode(codeStream);
        var codeBytes = codeStream.ToArray();

        var exceptionTable = new ExceptionTableEntry[TryCatchBlocks.Count];
        for (var i = 0; i < TryCatchBlocks.Count; i++)
        {
            var block = TryCatchBlocks[i];
            exceptionTable[i] = new ExceptionTableEntry
            {
                StartPc = new TUShort((ushort)GetLabelOffset(block.Start)),
                EndPc = new TUShort((ushort)GetLabelOffset(block.End)),
                HandlerPc = new TUShort((ushort)GetLabelOffset(block.Handler)),
                CatchType = new TUShort(0) // TODO: resolve CatchType to CP index
            };
        }

        return new CodeAttribute
        {
            MaxStack = new TUShort((ushort)MaxStack),
            MaxLocals = new TUShort((ushort)MaxLocals),
            Code = codeBytes,
            ExceptionTable = exceptionTable,
            Attributes = Attributes.ToArray()
        };
    }

    private int GetLabelOffset(Label label)
    {
        if (_labelOffsets.TryGetValue(label, out var offset))
        {
            return offset;
        }

        if (label.Offset.HasValue)
        {
            return label.Offset.Value;
        }

        throw new InvalidOperationException($"Label '{label}' has not been resolved.");
    }
}
