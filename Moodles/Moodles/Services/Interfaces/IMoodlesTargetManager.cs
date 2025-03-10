using Moodles.Moodles.MoodleUsers.Interfaces;

namespace Moodles.Moodles.Services.Interfaces;

internal interface IMoodlesTargetManager
{
    IMoodleHolder? Target { get; }
    IMoodleHolder? FocusTarget { get; }
    IMoodleHolder? MouseOverTarget { get; }
    IMoodleHolder? PreviousTarget { get; }
    IMoodleHolder? GPoseTarget { get; }
    IMoodleHolder? MouseOverNameplateTarget { get; }
}
