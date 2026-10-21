using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Domain.Rewards;

public sealed class MatchRewardDefinition
{
    private readonly List<MatchRewardDefinitionItem> _items = [];

    public Guid Id { get; private set; }

    public Guid MatchId { get; private set; }

    public string CurrencyCode { get; private set; } = null!;

    public long ParticipationAmount { get; private set; }

    public long WinBonusAmount { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<MatchRewardDefinitionItem> Items => _items;

    // EF Core
    private MatchRewardDefinition() { }

    public static Result<MatchRewardDefinition, Error> Create(Guid matchId, MatchRewardData data)
    {
        string currency = (data.CurrencyCode ?? string.Empty).Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(currency)
            || currency.Length > MatchmakingConstants.CURRENCY_MAX_LENGTH)
        {
            return MatchmakingErrors.RewardCurrencyInvalid;
        }

        if (data.ParticipationAmount < 0 || data.WinBonusAmount < 0)
        {
            return MatchmakingErrors.RewardAmountInvalid;
        }

        if (data.Items.Count > MatchmakingConstants.MAX_REWARD_ITEMS)
        {
            return MatchmakingErrors.RewardItemsInvalid;
        }

        var seenItems = new HashSet<Guid>();

        foreach (MatchRewardItemData item in data.Items)
        {
            if (item.ItemId == Guid.Empty
                || string.IsNullOrWhiteSpace(item.Code)
                || string.IsNullOrWhiteSpace(item.Kind)
                || item.Quantity <= 0
                || !seenItems.Add(item.ItemId))
            {
                return MatchmakingErrors.RewardItemsInvalid;
            }
        }

        var definition = new MatchRewardDefinition
        {
            Id = Guid.CreateVersion7(),
            MatchId = matchId,
            CurrencyCode = currency,
            ParticipationAmount = data.ParticipationAmount,
            WinBonusAmount = data.WinBonusAmount,
            CreatedAt = DateTime.UtcNow,
        };

        foreach (MatchRewardItemData item in data.Items)
        {
            definition._items.Add(new MatchRewardDefinitionItem(definition.Id, item));
        }

        return definition;
    }
}
