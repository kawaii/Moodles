namespace Moodles.Data;
public class FlyPopupTextData
{
    public MyStatus Status;
    public bool IsAddition;
    public uint Owner;

    public FlyPopupTextData(MyStatus status, bool isAddition, uint owner)
    {
        Status = status;
        IsAddition = isAddition;
        Owner = owner;
    }
}
