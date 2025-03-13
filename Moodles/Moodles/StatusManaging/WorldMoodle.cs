using System;
using Dalamud.Plugin.Services;
using MemoryPack;
using Moodles.Moodles.Mediation;
using Moodles.Moodles.Mediation.Interfaces;
using Moodles.Moodles.StatusManaging.Interfaces;

namespace Moodles.Moodles.StatusManaging;

[MemoryPackable]
[Serializable]
internal partial class WorldMoodle : IWorldMoodle
{
    public Guid Identifier { get; set; }
    public uint StackCount { get; set; }
    public long AppliedOn { get; set; }
    public long TickedTime { get; set; }
    public ulong AppliedBy { get; set; }

    public void Update(IFramework framework)
    {
        TickedTime += framework.UpdateDelta.Ticks;
    }

    public void AddStacksUnchecked(uint stacks, bool resetTime, IMoodlesMediator? mediator = null)
    {
        StackCount += stacks;
        if (resetTime)
        {
            AppliedOn = DateTime.Now.Ticks;
            TickedTime = 0;
        }
        mediator?.Send(new MoodleStackChangedMessage(this));
    }
}
