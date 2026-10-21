namespace MatchmakingService.Domain.Rewards;

public sealed class MatchRewardGrantItem
{
    public Guid Id { get; private set; }

    public Guid GrantId { get; private set; }

    public Guid ItemId { get; private set; }

    public string Code { get; private set; } = null!;

    public string Kind { get; private set; } = null!;

    public string? Slot { get; private set; }

    public int Quantity { get; private set; }

    // EF Core
    private MatchRewardGrantItem() { }

    internal MatchRewardGrantItem(Guid grantId, MatchRewardItemData data)
    {
        Id = Guid.CreateVersion7();
        GrantId = grantId;
        ItemId = data.ItemId;
        Code = data.Code;
        Kind = data.Kind;
        Slot = data.Slot;
        Quantity = data.Quantity;
    }
}
