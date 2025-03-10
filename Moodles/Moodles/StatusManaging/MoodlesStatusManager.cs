using System;
using Dalamud.Plugin.Services;
using MemoryPack;
using Moodles.Moodles.Mediation;
using Moodles.Moodles.Mediation.Interfaces;
using Moodles.Moodles.StatusManaging.Interfaces;
using Newtonsoft.Json;

namespace Moodles.Moodles.StatusManaging;

[Serializable]
[MemoryPackable]
internal partial class MoodlesStatusManager : IMoodleStatusManager
{
    [MemoryPackIgnore][JsonIgnore] public bool Enabled { get; set; } = true;

    [MemoryPackIgnore][JsonIgnore] public bool IsActive { get; private set; }
    [MemoryPackIgnore][JsonIgnore] public bool IsEphemeral { get; private set; } = false;

    public ulong ContentID { get; set; }     // The owners contentID if this is a pets status manager

    public int SkeletonID { get; set; }      // The pets skeleton, is 0 if it is a player

    [MemoryPackConstructor]
    [JsonConstructor]
    public MoodlesStatusManager(ulong contentID, int skeletonID) : this(contentID, skeletonID, true) { }
    public MoodlesStatusManager(ulong contentID, int skeletonID, bool active)
    {
        ContentID = contentID;
        SkeletonID = skeletonID;
        IsActive = active;

        SetEphemeralStatus(!IsActive);
    }

    public void Update(IFramework framework)
    {

    }

    public void SetActive(bool active, IMoodlesMediator? mediator = null)
    {
        IsActive = active;

        mediator?.Send(new StatusManagerDirtyMessage(this));
    }

    public void SetEphemeralStatus(bool ephemeralStatus, IMoodlesMediator? mediator = null)
    {
        IsEphemeral = ephemeralStatus;

        mediator?.Send(new StatusManagerDirtyMessage(this));
    }

    public void Clear(IMoodlesMediator? mediator = null)
    {
        SetActive(false);
    }

    public bool Savable()
    {
        if (IsEphemeral) return false;
        if (!IsActive) return false;
        if (SkeletonID > 0) return false;   // Dont save status managers of minions

        return true;
    }
}
