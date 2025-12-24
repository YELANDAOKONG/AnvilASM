using Anvil.Interfaces;
using Anvil.Structures.Attributes.Records;
using Anvil.Types;

namespace Anvil.Structures.Attributes;

public class RecordAttribute : IStructure<RecordAttribute>, IAttribute
{
    public TUShort ComponentsCount { get; set; }
    public RecordComponentInfo[] Components { get; set; } = Array.Empty<RecordComponentInfo>();

    public void Write(Stream stream)
    {
        new TUShort((ushort)Components.Length).Write(stream);
        foreach (var comp in Components) comp.Write(stream);
    }

    public static RecordAttribute Read(Stream stream)
    {
        var attr = new RecordAttribute();
        attr.ComponentsCount = TUShort.Read(stream);
        attr.Components = new RecordComponentInfo[attr.ComponentsCount.Value];
        for (int i = 0; i < attr.Components.Length; i++)
        {
            attr.Components[i] = RecordComponentInfo.Read(stream);
        }
        return attr;
    }
}