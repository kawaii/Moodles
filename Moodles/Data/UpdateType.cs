namespace Moodles.Data;

/// <summary>
/// Determines how the status manager should add/update a status upon application.
/// </summary>
public enum UpdateSource
{
    // From full manager updates. These should ignore stackOnReapply, as they are updates, not reapplications.
    DataString,
    // From Applications or Reapplications. These should respect other "on reapply" settings.
    StatusTuple,
}
