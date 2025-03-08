using Moodles.Moodles.StatusManaging.Interfaces;
using System;

namespace Moodles.Moodles.MoodleUsers.Interfaces;

internal interface IMoodleHolder : IDisposable
{
    IMoodleStatusManager StatusManager { get; }
}
