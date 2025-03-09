using Moodles.Moodles.Mediation.Interfaces;
using Moodles.Moodles.StatusManaging.Enums;
using System;

namespace Moodles.Moodles.StatusManaging.Interfaces;

internal interface IMoodle
{
    Guid Identifier { get; }

    string Title { get; }
    string Description { get; }
    bool Dispellable { get; }
    StatusType StatusType { get; }
    int IconID { get; }
    string VFXPath { get; }
    bool DispellsOnDeath { get; }
    bool CountsDownWhenOffline { get; }
    int StartingStacks { get; }
    
    Guid StatusOnDispell { get; }
    
    bool StackOnReapply { get; }
    int StackIncrementOnReapply { get; }

    int Days { get; }
    int Hours { get; }
    int Minutes { get; }
    int Seconds { get; }

    bool Permanent { get; }

    bool IsEphemeral { get; }

    void SetIdentifier(Guid identifier, IMoodlesMediator mediator);
    void SetTitle(string title, IMoodlesMediator mediator);
    void SetDescription(string description, IMoodlesMediator mediator);
    void SetDispellable(bool dispellable, IMoodlesMediator mediator);
    void SetStatusType(StatusType statusType, IMoodlesMediator mediator);
    void SetIconID(int iconID, IMoodlesMediator mediator);
    void SetVFXPath(string vfxPath, IMoodlesMediator mediator);
    void SetDispellsOnDeath(bool dispellsOnDeath, IMoodlesMediator mediator);
    void SetCountsDownWhenOffline(bool countsDownWhenOffline, IMoodlesMediator mediator);
    void SetStartingStacks(int startingStacks, IMoodlesMediator mediator);
    void SetStatusOnDispell(Guid statusOnDispell, IMoodlesMediator mediator);
    void SetStackOnReapply(bool stackOnReapply, IMoodlesMediator mediator);
    void SetStackIncrementOnReapply(int stackIncrementOnReapply, IMoodlesMediator mediator);
    void SetDuration(int days, int hours, int minutes, int seconds, IMoodlesMediator mediator);
    void SetPermanent(bool permanent, IMoodlesMediator mediator);
    void SetEphemeral(bool isEphemeral, IMoodlesMediator mediator);

    // The actual moodle thing should hold data like
    // Current Stack
    // GUID
    // Applier Content ID, Name and Homeworld
    // Time ofc
    // Thats about it really
}
