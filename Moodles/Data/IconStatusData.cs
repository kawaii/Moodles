namespace Moodles.Data;
public record struct IconStatusData
{
    public uint StatusId;
    public string Name;
    public uint StackCount;

    public IconStatusData(uint statusId, string name, uint stackCount)
    {
        StatusId = statusId;
        Name = name;
        StackCount = stackCount;
    }
}
