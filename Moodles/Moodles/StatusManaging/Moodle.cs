using MemoryPack;
using Moodles.Moodles.StatusManaging.Enums;
using Moodles.Moodles.StatusManaging.Interfaces;
using System;
using Newtonsoft.Json;
using Moodles.Moodles.Mediation.Interfaces;
using Moodles.Moodles.Mediation;

namespace Moodles.Moodles.StatusManaging;

[Serializable]
[MemoryPackable]
internal partial class Moodle : IMoodle
{
    public Guid Identifier { get; private set; } = Guid.NewGuid();
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool Dispellable { get; private set; } = false;
    public StatusType StatusType { get; private set; } = StatusType.Positive;
    public int IconID { get; private set; } = 0;
    public string VFXPath { get; private set; } = string.Empty;
    public bool DispellsOnDeath { get; private set; } = true;
    public bool CountsDownWhenOffline { get; private set; } = false;
    public int StartingStacks { get; private set; } = 1;
    public Guid StatusOnDispell { get; private set; } = Guid.Empty;
    public bool StackOnReapply { get; private set; } = false;
    public int StackIncrementOnReapply { get; private set; } = 1;
    public int Days { get; private set; } = 0;
    public int Hours { get; private set; } = 0;
    public int Minutes { get; private set; } = 0;
    public int Seconds { get; private set; } = 0;
    public bool Permanent { get; private set; } = true;

    [MemoryPackIgnore] [JsonIgnore] public bool IsEphemeral { get; private set; } = true;

    public void SetIdentifier(Guid identifier, IMoodlesMediator mediator)
    {
        Identifier = identifier;
        mediator.Send(new MoodleChangedMessage(this));
    }

    public void SetTitle(string title, IMoodlesMediator mediator)
    {
        Title = title;
        mediator.Send(new MoodleChangedMessage(this));
    }

    public void SetDescription(string description, IMoodlesMediator mediator)
    {
        Description = description;
        mediator.Send(new MoodleChangedMessage(this));
    }

    public void SetDispellable(bool dispellable, IMoodlesMediator mediator)
    {
        Dispellable = dispellable;
        mediator.Send(new MoodleChangedMessage(this));
    }

    public void SetStatusType(StatusType statusType, IMoodlesMediator mediator)
    {
        StatusType = statusType;
        mediator.Send(new MoodleChangedMessage(this));
    }

    public void SetIconID(int iconID, IMoodlesMediator mediator)
    {
        IconID = iconID;
        mediator.Send(new MoodleChangedMessage(this));
    }

    public void SetVFXPath(string vfxPath, IMoodlesMediator mediator)
    {
        VFXPath = vfxPath;
        mediator.Send(new MoodleChangedMessage(this));
    }

    public void SetDispellsOnDeath(bool dispellsOnDeath, IMoodlesMediator mediator)
    {
        DispellsOnDeath = dispellsOnDeath;
        mediator.Send(new MoodleChangedMessage(this));
    }

    public void SetCountsDownWhenOffline(bool countsDownWhenOffline, IMoodlesMediator mediator)
    {
        CountsDownWhenOffline = countsDownWhenOffline;
        mediator.Send(new MoodleChangedMessage(this));
    }

    public void SetStartingStacks(int startingStacks, IMoodlesMediator mediator)
    {
        StartingStacks = startingStacks;
        mediator.Send(new MoodleChangedMessage(this));
    }

    public void SetStatusOnDispell(Guid statusOnDispell, IMoodlesMediator mediator)
    {
        StatusOnDispell = statusOnDispell;
        mediator.Send(new MoodleChangedMessage(this));
    }

    public void SetStackOnReapply(bool stackOnReapply, IMoodlesMediator mediator)
    {
        StackOnReapply = stackOnReapply;
        mediator.Send(new MoodleChangedMessage(this));
    }

    public void SetStackIncrementOnReapply(int stackIncrementOnReapply, IMoodlesMediator mediator)
    {
        StackIncrementOnReapply = stackIncrementOnReapply;
        mediator.Send(new MoodleChangedMessage(this));
    }

    public void SetDuration(int days, int hours, int minutes, int seconds, IMoodlesMediator mediator)
    {
        Days = days;
        Hours = hours;
        Minutes = minutes;
        Seconds = seconds;
        mediator.Send(new MoodleChangedMessage(this));
    }

    public void SetPermanent(bool permanent, IMoodlesMediator mediator)
    {
        Permanent = permanent;
        mediator.Send(new MoodleChangedMessage(this));
    }

    public void SetEphemeral(bool isEphemeral, IMoodlesMediator mediator)
    {
        IsEphemeral = isEphemeral;
        mediator.Send(new MoodleChangedMessage(this));
    }
}
