namespace ClanService.Domain.Requests;

public enum ClanRequestKind
{
    /// <summary>
    /// The player asks to join; an officer or the leader decides.
    /// </summary>
    Request = 1,

    /// <summary>
    /// An officer or the leader invites; the player decides.
    /// </summary>
    Invite = 2,
}

public enum ClanRequestStatus
{
    Pending = 1,
    Accepted = 2,
    Declined = 3,
    Cancelled = 4,
}
