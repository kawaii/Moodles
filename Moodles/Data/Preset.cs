namespace Moodles.Data;
[Serializable]
public class Preset
{
    internal string ID => GUID.ToString();
    public Guid GUID = Guid.NewGuid();
    public List<Guid> Statuses = [];
    public PresetApplicationType ApplicationType = PresetApplicationType.UpdateExisting;
    public string Title = "";
    public bool ShouldSerializeGUID() => GUID != Guid.Empty;

    public MoodlePresetInfo ToPresetInfoTuple() => (GUID, Statuses, ApplicationType, Title);
}
