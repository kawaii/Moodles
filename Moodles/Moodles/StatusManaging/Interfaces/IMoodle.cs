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

    // The actual moodle thing should hold data like
    // Current Stack
    // GUID
    // Applier Content ID, Name and Homeworld
    // Time ofc
    // Thats about it really
}
