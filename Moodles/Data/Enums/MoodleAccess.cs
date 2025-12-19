namespace Moodles.Data;

/// <summary>
///     Defines access permissions for moodle application and removal on others.
/// </summary>
[Flags]
public enum MoodleAccess : short
{
    None            = 0 << 0, // No Access
    AllowOwn        = 1 << 0, // The Access Owners own moodles can be applied.
    AllowOther      = 1 << 1, // The Access Owners 'other' / 'pair' can apply their moodles.
    Positive        = 1 << 2, // Positive Statuses can be applied.
    Negative        = 1 << 3, // Negative Statuses can be applied.
    Special         = 1 << 4, // Special Statuses can be applied.
    Permanent       = 1 << 5, // Moodles without a duration can be applied.
    RemoveApplied   = 1 << 6, // 'Other' can remove only moodles they have applied.
    RemoveAny       = 1 << 7, // 'Other' can remove any moodles.
    Clearing        = 1 << 8, // 'Other' can clear all moodles.
}