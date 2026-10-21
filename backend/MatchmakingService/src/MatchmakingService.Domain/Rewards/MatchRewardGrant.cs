namespace MatchmakingService.Domain.Rewards;

public sealed class MatchRewardGrant
{
    private readonly List<MatchRewardGrantItem> _items = [];

    public Guid Id { get; private set; }

    public Guid MatchId { get; private set; }

    public Guid AccountId { get; private set; }

    public string CurrencyCode { get; private set; } = null!;

    public long Amount { get; private set; }

    public MatchRewardStatus Status { get; private set; }

    public Guid WalletOperationId { get; private set; }

    public DateTime? WalletCreditedAt { get; private set; }

    public DateTime? ItemsPublishedAt { get; private set; }

    public DateTime? ItemsGrantedAt { get; private set; }

    public string? FailureReason { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<MatchRewardGrantItem> Items => _items;

    // EF Core
    private MatchRewardGrant() { }

    private MatchRewardGrant(
        Guid matchId,
        Guid accountId,
        MatchRewardDefinition definition,
        long amount,
        DateTime now)
    {
        Id = Guid.CreateVersion7();
        MatchId = matchId;
        AccountId = accountId;
        CurrencyCode = definition.CurrencyCode;
        Amount = amount;
        Status = MatchRewardStatus.Pending;
        WalletOperationId = Guid.CreateVersion7();
        CreatedAt = now;
        UpdatedAt = now;

        foreach (MatchRewardDefinitionItem item in definition.Items)
        {
            _items.Add(new MatchRewardGrantItem(
                Id,
                new MatchRewardItemData(item.ItemId, item.Code, item.Kind, item.Slot, item.Quantity)));
        }

        if (_items.Count == 0)
        {
            ItemsPublishedAt = now;
            ItemsGrantedAt = now;
        }
    }

    public static MatchRewardGrant Create(
        Guid matchId,
        Guid accountId,
        MatchRewardDefinition definition,
        long amount,
        DateTime now) =>
        new(matchId, accountId, definition, amount, now);

    public void MarkWalletCredited(DateTime now)
    {
        WalletCreditedAt = now;
        UpdatedAt = now;
        RefreshStatus(now);
    }

    public void MarkItemsPublished(DateTime now)
    {
        ItemsPublishedAt = now;
        UpdatedAt = now;
    }

    public void MarkItemsGranted(DateTime now)
    {
        ItemsGrantedAt = now;
        FailureReason = null;
        UpdatedAt = now;
        RefreshStatus(now);
    }

    public void MarkFailed(string reason, DateTime now)
    {
        Status = MatchRewardStatus.Failed;
        FailureReason = reason.Length > MatchmakingConstants.FAILURE_REASON_MAX_LENGTH
            ? reason[..MatchmakingConstants.FAILURE_REASON_MAX_LENGTH]
            : reason;
        UpdatedAt = now;
    }

    public void RecordFailure(string reason, DateTime now)
    {
        FailureReason = reason.Length > MatchmakingConstants.FAILURE_REASON_MAX_LENGTH
            ? reason[..MatchmakingConstants.FAILURE_REASON_MAX_LENGTH]
            : reason;
        UpdatedAt = now;
    }

    private void RefreshStatus(DateTime now)
    {
        if (Status == MatchRewardStatus.Failed)
        {
            return;
        }

        if (WalletCreditedAt is not null && ItemsGrantedAt is not null)
        {
            Status = MatchRewardStatus.Granted;
            UpdatedAt = now;
        }
    }
}
