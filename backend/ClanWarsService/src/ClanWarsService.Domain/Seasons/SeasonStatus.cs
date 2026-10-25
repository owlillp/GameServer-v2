namespace ClanWarsService.Domain.Seasons;

/// <summary>
/// Draft → Scheduled (published) → Active → Finalizing → Rewarding → Closed.
/// Everything after Draft follows the schedule: a phase job moves the stored status to the
/// phase the clock says, applying every missed transition in order.
/// </summary>
public enum SeasonStatus
{
    Draft = 1,
    Scheduled = 2,
    Active = 3,
    Finalizing = 4,
    Rewarding = 5,
    Closed = 6,
}
