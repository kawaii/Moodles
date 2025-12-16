namespace Moodles.Data;
public class FlyPopupTextData
{
    public MyStatus Status;
    public bool IsAddition;
    public uint OwnerEntityId;

    public FlyPopupTextData(MyStatus status, bool isAddition, uint owner)
    {
        Status = status;
        IsAddition = isAddition;
        OwnerEntityId = owner;
    }
}
