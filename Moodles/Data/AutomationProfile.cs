namespace Moodles.Data;
[Serializable]
public class AutomationProfile
{
    public Guid GUID = Guid.NewGuid();
    public string Name = "";
    public uint World = 0;
    public bool Enabled = true;
    public string Character = "";
    public List<AutomationCombo> Combos = [];
}
