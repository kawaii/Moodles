using Lumina.Excel.Sheets;

namespace Moodles.Moodles.Services.Data;

internal readonly struct IconInfo
{
    public readonly uint IconID;
    public readonly string Name;
    public readonly StatusType Type;
    public readonly bool IsStackable;
    public readonly ClassJobCategory ClassJobCategory;
    public readonly bool IsFCBuff;
    public readonly string Description;

    public IconInfo(uint iconId, string name, StatusType type, bool isStackable, ClassJobCategory classJobCategory, bool isFcBuff, string description)
    {
        IconID = iconId;
        Name = name;
        Type = type;
        IsStackable = isStackable;
        ClassJobCategory = classJobCategory;
        IsFCBuff = isFcBuff;
        Description = description;
    }
}

