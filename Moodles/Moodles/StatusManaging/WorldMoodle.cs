using System;
using MemoryPack;
using Moodles.Moodles.StatusManaging.Interfaces;

namespace Moodles.Moodles.StatusManaging;

[MemoryPackable]
[Serializable]
internal partial class WorldMoodle : IWorldMoodle
{
    public Guid Identifier { get; set; }
    public uint StackCount { get; set; }
    public DateTime AppliedOn { get; set; }

    public WorldMoodle()
    {

    }
}
