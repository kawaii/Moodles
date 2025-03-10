using Moodles.Moodles.Updating.Interfaces;
using System;

namespace Moodles.Moodles.StatusManaging.Interfaces;

internal interface IWorldMoodle : IUpdatable
{
    Guid Identifier { get; }
    uint StackCount { get; }
    ulong AppliedOn { get; }
    ulong TickedTime { get; }
    ulong AppliedBy { get; }
}
