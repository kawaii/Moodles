using Dalamud.Plugin.Services;
using Moodles.Moodles.Mediation.Interfaces;
using Moodles.Moodles.Updating.Interfaces;
using System;

namespace Moodles.Moodles.StatusManaging.Interfaces;

internal interface IWorldMoodle
{
    Guid Identifier { get; }
    uint StackCount { get; }
    long AppliedOn { get; }
    long TickedTime { get; }
    ulong AppliedBy { get; }

    void Update(IFramework framework);
    void AddStacksUnchecked(uint stacks, bool resetTime, IMoodlesMediator? mediator = null);
}
