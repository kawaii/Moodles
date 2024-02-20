using ECommons.ExcelServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moodles.Data;
[Serializable]
public class AutomationCombo
{
    public Guid Preset = Guid.Empty;
    public List<Job> Jobs = [];
}
