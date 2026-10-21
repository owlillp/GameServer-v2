namespace MatchmakingService.Domain.Rewards;

public sealed class MatchRewardDefinitionItem
{
    public Guid Id { get; private set; }

    public Guid DefinitionId { get; private set; }

    public Guid ItemId { get; private set; }

    public string Code { get; private set; } = null!;

    public string Kind { get; private set; } = null!;

    public string? Slot { get; private set; }

    public int Quantity { get; private set; }

    // EF Core
    private MatchRewardDefinitionItem() { }

    internal MatchRewardDefinitionItem(Guid definitionId, MatchRewardItemData data)
    {
        Id = Guid.CreateVersion7();
        DefinitionId = definitionId;
        ItemId = data.ItemId;
        Code = data.Code;
        Kind = data.Kind;
        Slot = data.Slot;
        Quantity = data.Quantity;
    }
}
