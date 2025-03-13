using Dalamud.Plugin.Services;
using Moodles.Moodles.Mediation.Interfaces;
using Moodles.Moodles.MoodleUsers.Interfaces;
using Moodles.Moodles.Services.Data;
using Moodles.Moodles.Services.Interfaces;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Moodles.Moodles.StatusManaging.Interfaces;

internal interface IMoodleStatusManager
{
    bool IsEphemeral { get; }

    ulong ContentID { get; } // Player or owner content ID
    int SkeletonID { get; } // Player skeleton is 0

    List<WorldMoodle> WorldMoodles { get; }

    void Update(IFramework framework);
    void ValidateMoodles(IFramework framework, IMoodleValidator validator, IMoodlesDatabase database, IMoodleUser? user, IMoodlesMediator? mediator = null);

    void SetEphemeralStatus(bool ephemeralStatus, IMoodlesMediator? mediator = null);

    void Clear(IMoodlesMediator? mediator = null);

    bool Savable();

    bool HasMoodle(IMoodle moodle, [NotNullWhen(true)] out WorldMoodle? wMoodle);
    bool HasMaxedOutMoodle(IMoodle moodle, IMoodleValidator moodleValidator, [NotNullWhen(true)] out WorldMoodle? wMoodle);
    void ApplyMoodle(IMoodle moodle, IMoodleValidator moodleValidator, IUserList userList, IMoodlesMediator? mediator = null);
    void RemoveMoodle(IMoodle moodle, MoodleRemoveReason removeReason, IMoodlesMediator? mediator = null);
    void RemoveMoodle(WorldMoodle wMoodle, MoodleRemoveReason removeReason, IMoodlesMediator? mediator = null);
}
