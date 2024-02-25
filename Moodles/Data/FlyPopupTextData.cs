namespace Moodles.Data;
public class FlyPopupTextData
{
    public MyStatus Status;
    public bool IsAddition;
    public uint Owner;

    public FlyPopupTextData(MyStatus status, bool isAddition, uint owner)
    {
        this.Status = status;
        this.IsAddition = isAddition;
        this.Owner = owner;
    }
}
