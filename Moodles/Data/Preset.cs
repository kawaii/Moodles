namespace Moodles.Data;
[Serializable]
public class Preset
{
    internal string ID => GUID.ToString();
    public Guid GUID = Guid.NewGuid();
    public List<Guid> Statuses = [];
    public PresetApplicationType ApplicationType = PresetApplicationType.UpdateExisting;
    public bool ShouldSerializeGUID() => GUID != Guid.Empty;
}
