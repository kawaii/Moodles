using MemoryPack;
using Moodles.Moodles.StatusManaging.Interfaces;
using System;
using Newtonsoft.Json;
using Moodles.Moodles.Mediation.Interfaces;
using Moodles.Moodles.Mediation;
using Moodles.Moodles.Services.Data;

namespace Moodles.Moodles.StatusManaging;

[Serializable]
[MemoryPackable]
internal partial class Moodle : IMoodle
{
    [JsonIgnore] public string ID => Identifier.ToString();

    public Guid Identifier { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Dispellable { get; set; } = false;
    public StatusType StatusType { get; set; } = StatusType.Positive;
    public int IconID { get; set; } = 0;
    public string VFXPath { get; set; } = string.Empty;
    public bool DispellsOnDeath { get; set; } = true;
    public bool CountsDownWhenOffline { get; set; } = false;
    public int StartingStacks { get; set; } = 1;
    public Guid StatusOnDispell { get; set; } = Guid.Empty;
    public bool StackOnReapply { get; set; } = false;
    public int StackIncrementOnReapply { get; set; } = 1;
    public int Days { get; set; } = 0;
    public int Hours { get; set; } = 0;
    public int Minutes { get; set; } = 0;
    public int Seconds { get; set; } = 0;
    public bool Permanent { get; set; } = true;
    public ulong CreatedBy { get; set; } = 0;

    [MemoryPackIgnore] [JsonIgnore] public bool IsEphemeral { get; set; } = true;

    public void SetIdentifier(Guid identifier, IMoodlesMediator? mediator = null)
    {
        Identifier = identifier;
        mediator?.Send(new MoodleChangedMessage(this));
    }

    public void SetTitle(string title, IMoodlesMediator? mediator = null)
    {
        Title = title;
        mediator?.Send(new MoodleChangedMessage(this));
    }

    public void SetDescription(string description, IMoodlesMediator? mediator = null)
    {
        Description = description;
        mediator?.Send(new MoodleChangedMessage(this));
    }

    public void SetDispellable(bool dispellable, IMoodlesMediator? mediator = null)
    {
        Dispellable = dispellable;
        mediator?.Send(new MoodleChangedMessage(this));
    }

    public void SetStatusType(StatusType statusType, IMoodlesMediator? mediator = null)
    {
        StatusType = statusType;
        mediator?.Send(new MoodleChangedMessage(this));
    }

    public void SetIconID(int iconID, IMoodlesMediator? mediator = null)
    {
        IconID = iconID;
        mediator?.Send(new MoodleChangedMessage(this));
    }

    public void SetVFXPath(string vfxPath, IMoodlesMediator? mediator = null)
    {
        VFXPath = vfxPath;
        mediator?.Send(new MoodleChangedMessage(this));
    }

    public void SetDispellsOnDeath(bool dispellsOnDeath, IMoodlesMediator? mediator = null)
    {
        DispellsOnDeath = dispellsOnDeath;
        mediator?.Send(new MoodleChangedMessage(this));
    }

    public void SetCountsDownWhenOffline(bool countsDownWhenOffline, IMoodlesMediator? mediator = null)
    {
        CountsDownWhenOffline = countsDownWhenOffline;
        mediator?.Send(new MoodleChangedMessage(this));
    }

    public void SetStartingStacks(int startingStacks, IMoodlesMediator? mediator = null)
    {
        StartingStacks = startingStacks;
        mediator?.Send(new MoodleChangedMessage(this));
    }

    public void SetStatusOnDispell(Guid statusOnDispell, IMoodlesMediator? mediator = null)
    {
        StatusOnDispell = statusOnDispell;
        mediator?.Send(new MoodleChangedMessage(this));
    }

    public void SetStackOnReapply(bool stackOnReapply, IMoodlesMediator? mediator = null)
    {
        StackOnReapply = stackOnReapply;
        mediator?.Send(new MoodleChangedMessage(this));
    }

    public void SetStackIncrementOnReapply(int stackIncrementOnReapply, IMoodlesMediator? mediator = null)
    {
        StackIncrementOnReapply = stackIncrementOnReapply;
        mediator?.Send(new MoodleChangedMessage(this));
    }

    public void SetDuration(int days, int hours, int minutes, int seconds, IMoodlesMediator? mediator)
    {
        Days = days;
        Hours = hours;
        Minutes = minutes;
        Seconds = seconds;
        mediator?.Send(new MoodleChangedMessage(this));
    }

    public void SetPermanent(bool permanent, IMoodlesMediator? mediator = null)
    {
        Permanent = permanent;
        mediator?.Send(new MoodleChangedMessage(this));
    }

    public void SetEphemeral(bool isEphemeral, IMoodlesMediator? mediator = null)
    {
        IsEphemeral = isEphemeral;
        mediator?.Send(new MoodleChangedMessage(this));
    }

    public void SetCreatedBy(ulong createdBy, IMoodlesMediator? mediator = null)
    {
        CreatedBy = createdBy;
        mediator?.Send(new MoodleChangedMessage(this));
    }
}
