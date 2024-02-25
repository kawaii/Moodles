using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moodles.Data;
public record struct IconStatusData
{
    public uint StatusId;
    public string Name;
    public uint StackCount;

    public IconStatusData(uint statusId, string name, uint stackCount)
    {
        this.StatusId = statusId;
        this.Name = name;
        this.StackCount = stackCount;
    }
}
