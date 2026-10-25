namespace ClanWarsService.Domain.Rewards;

public enum RewardGrantStatus
{
    Pending = 1,
    Granted = 2,
    Failed = 3,
}

/// <summary>
/// One reward to deliver: clan points to a clan (<see cref="AccountId"/> is null) or currency and
/// items to one member. Unique by (season, clan, account), so closing a season twice cannot
/// double it. Every delivery step is idempotent by the grant id: the ClanService ledger, the
/// Wallet credit and the Inventory receipt all use it as operation id.
/// </summary>
public sealed class SeasonRewardGrant
{
    private static readonly TimeSpan MAX_BACKOFF = TimeSpan.FromHours(1);

    public Guid Id { get; private set; }

    public Guid SeasonId { get; private set; }

    public Guid ClanId { get; private set; }

    public Guid? AccountId { get; private set; }

    public int Rank { get; private set; }

    public long ClanPoints { get; private set; }

    public string? CurrencyCode { get; private set; }

    public long CurrencyAmount { get; private set; }

    public IReadOnlyList<RewardItem> Items { get; private set; } = [];

    public RewardGrantStatus Status { get; private set; }

    public int Attempts { get; private set; }

    public DateTime NextAttemptAt { get; private set; }

    public string? LastError { get; private set; }

    public DateTime? PointsPublishedAt { get; private set; }

    public DateTime? WalletCreditedAt { get; private set; }

    public DateTime? ItemsPublishedAt { get; private set; }

    public DateTime? ItemsGrantedAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public bool NeedsPoints => ClanPoints > 0 && PointsPublishedAt is null;

    public bool NeedsWallet => CurrencyAmount > 0 && CurrencyCode is not null && WalletCreditedAt is null;

    public bool NeedsItemsPublish => Items.Count > 0 && ItemsPublishedAt is null;

    // EF Core
    private SeasonRewardGrant() { }

    public static SeasonRewardGrant ForClan(Guid seasonId, Guid clanId, int rank, long clanPoints, DateTime now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            SeasonId = seasonId,
            ClanId = clanId,
            Rank = rank,
            ClanPoints = clanPoints,
            Status = RewardGrantStatus.Pending,
            NextAttemptAt = now,
            CreatedAt = now,
        };

    public static SeasonRewardGrant ForMember(Guid seasonId, Guid clanId, Guid accountId, int rank, RewardTier tier, DateTime now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            SeasonId = seasonId,
            ClanId = clanId,
            AccountId = accountId,
            Rank = rank,
            CurrencyCode = tier.CurrencyCode,
            CurrencyAmount = tier.CurrencyCode is null ? 0 : tier.CurrencyAmount,
            Items = tier.Items,
            Status = RewardGrantStatus.Pending,
            NextAttemptAt = now,
            CreatedAt = now,
        };

    public void MarkPointsPublished(DateTime now)
    {
        PointsPublishedAt ??= now;
        Refresh(now);
    }

    public void MarkWalletCredited(DateTime now)
    {
        WalletCreditedAt ??= now;
        Refresh(now);
    }

    public void MarkItemsPublished(DateTime now)
    {
        ItemsPublishedAt ??= now;
        Refresh(now);
    }

    public void MarkItemsGranted(DateTime now)
    {
        ItemsPublishedAt ??= now;
        ItemsGrantedAt ??= now;
        Refresh(now);
    }

    /// <summary>
    /// A transient delivery failure: retry later with exponential backoff, give up after
    /// <paramref name="maxAttempts"/> (status Failed, alert).
    /// </summary>
    public void RecordFailure(string error, DateTime now, TimeSpan baseBackoff, int maxAttempts)
    {
        Attempts++;
        LastError = Truncate(error);

        if (Attempts >= maxAttempts)
        {
            Status = RewardGrantStatus.Failed;
            return;
        }

        double factor = Math.Pow(2, Attempts - 1);
        TimeSpan delay = TimeSpan.FromTicks((long)Math.Min(baseBackoff.Ticks * factor, MAX_BACKOFF.Ticks));
        NextAttemptAt = now + delay;
    }

    /// <summary>
    /// A permanent rejection (e.g. Inventory refused the items).
    /// </summary>
    public void Fail(string error)
    {
        LastError = Truncate(error);
        Status = RewardGrantStatus.Failed;
    }

    /// <summary>
    /// Admin retry of a failed grant; finished steps are not repeated.
    /// </summary>
    public void Retry(DateTime now)
    {
        if (Status != RewardGrantStatus.Failed)
        {
            return;
        }

        Status = RewardGrantStatus.Pending;
        Attempts = 0;
        NextAttemptAt = now;
    }

    /// <summary>
    /// Reconciliation found no Wallet operation for a credit marked done: credit again
    /// (idempotent by the grant id).
    /// </summary>
    public void ReopenWallet(DateTime now)
    {
        WalletCreditedAt = null;
        Reopen(now);
    }

    /// <summary>
    /// Items were sent long ago and never confirmed: send the command again (Inventory dedupes).
    /// </summary>
    public void ReopenItems(DateTime now)
    {
        ItemsPublishedAt = null;
        Reopen(now);
    }

    private void Reopen(DateTime now)
    {
        Status = RewardGrantStatus.Pending;
        CompletedAt = null;
        NextAttemptAt = now;
    }

    private void Refresh(DateTime now)
    {
        bool itemsDone = Items.Count == 0 || ItemsGrantedAt is not null;

        if (Status == RewardGrantStatus.Pending && !NeedsPoints && !NeedsWallet && itemsDone)
        {
            Status = RewardGrantStatus.Granted;
            CompletedAt = now;
        }
    }

    private static string Truncate(string error) => error.Length > 512 ? error[..512] : error;
}
