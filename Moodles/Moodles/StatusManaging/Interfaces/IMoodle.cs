using Moodles.Moodles.Mediation.Interfaces;
using Moodles.Moodles.Services.Data;
using System;

namespace Moodles.Moodles.StatusManaging.Interfaces;

internal interface IMoodle
{
    string ID { get; }

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
    ulong CreatedBy { get; }

    void SetIdentifier(Guid identifier, IMoodlesMediator? mediator = null);
    void SetTitle(string title, IMoodlesMediator? mediator = null);
    void SetDescription(string description, IMoodlesMediator? mediator = null);
    void SetDispellable(bool dispellable, IMoodlesMediator? mediator = null);
    void SetStatusType(StatusType statusType, IMoodlesMediator? mediator = null);
    void SetIconID(int iconID, IMoodlesMediator? mediator = null);
    void SetVFXPath(string vfxPath, IMoodlesMediator? mediator = null);
    void SetDispellsOnDeath(bool dispellsOnDeath, IMoodlesMediator? mediator = null);
    void SetCountsDownWhenOffline(bool countsDownWhenOffline, IMoodlesMediator? mediator = null);
    void SetStartingStacks(int startingStacks, IMoodlesMediator? mediator = null);
    void SetStatusOnDispell(Guid statusOnDispell, IMoodlesMediator? mediator = null);
    void SetStackOnReapply(bool stackOnReapply, IMoodlesMediator? mediator = null);
    void SetStackIncrementOnReapply(int stackIncrementOnReapply, IMoodlesMediator? mediator = null);
    void SetDuration(int days, int hours, int minutes, int seconds, IMoodlesMediator? mediator = null);
    void SetPermanent(bool permanent, IMoodlesMediator? mediator = null);
    void SetEphemeral(bool isEphemeral, IMoodlesMediator? mediator = null);
    void SetCreatedBy(ulong createdBy, IMoodlesMediator? mediator = null);

    // The actual moodle thing should hold data like
    // Current Stack
    // GUID
    // Applier Content ID, Name and Homeworld
    // Time ofc
    // Thats about it really
}
