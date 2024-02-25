using ECommons.ExcelServices;

namespace Moodles.Data;
[Serializable]
public class AutomationCombo
{
    public Guid Preset = Guid.Empty;
    public List<Job> Jobs = [];
}
