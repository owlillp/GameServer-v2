using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace ClanWarsService.Domain.Rewards;

public sealed record RewardItem(Guid ItemId, string Code, string Kind, string? Slot, int Quantity);

/// <summary>
/// Rewards for the ranks <see cref="FromRank"/>..<see cref="ToRank"/>: clan points to the clan,
/// currency and items to each member who fought at least <see cref="MinBattles"/> battles.
/// </summary>
public sealed class RewardTier
{
    public Guid Id { get; private set; }

    public Guid TableId { get; private set; }

    public int FromRank { get; private set; }

    public int ToRank { get; private set; }

    public long ClanPoints { get; private set; }

    public string? CurrencyCode { get; private set; }

    public long CurrencyAmount { get; private set; }

    public IReadOnlyList<RewardItem> Items { get; private set; } = [];

    public int MinBattles { get; private set; }

    // EF Core
    private RewardTier() { }

    internal RewardTier(Guid tableId, int fromRank, int toRank, long clanPoints, string? currencyCode, long currencyAmount, IReadOnlyList<RewardItem> items, int minBattles)
    {
        Id = Guid.CreateVersion7();
        TableId = tableId;
        FromRank = fromRank;
        ToRank = toRank;
        ClanPoints = clanPoints;
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? null : currencyCode.Trim();
        CurrencyAmount = currencyAmount;
        Items = items;
        MinBattles = minBattles;
    }

    public bool Covers(int rank) => rank >= FromRank && rank <= ToRank;

    public bool HasMemberReward => (CurrencyAmount > 0 && CurrencyCode is not null) || Items.Count > 0;
}

public sealed record RewardTierData(
    int FromRank,
    int ToRank,
    long ClanPoints,
    string? CurrencyCode,
    long CurrencyAmount,
    IReadOnlyList<RewardItem> Items,
    int MinBattles);

public sealed class RewardTable
{
    public const int NAME_MAX_LENGTH = 64;

    private readonly List<RewardTier> _tiers = [];

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<RewardTier> Tiers => _tiers;

    // EF Core
    private RewardTable() { }

    public static Result<RewardTable, Error> Create(string name, IReadOnlyList<RewardTierData> tiers, DateTime now)
    {
        string trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length is 0 or > NAME_MAX_LENGTH || tiers.Count == 0)
        {
            return ClanWarsErrors.RewardTableInvalid;
        }

        RewardTierData[] ordered = [.. tiers.OrderBy(tier => tier.FromRank)];
        for (int index = 0; index < ordered.Length; index++)
        {
            RewardTierData tier = ordered[index];
            bool invalid = tier.FromRank < 1
                || tier.ToRank < tier.FromRank
                || tier.ClanPoints < 0
                || tier.CurrencyAmount < 0
                || tier.MinBattles < 0
                || (tier.CurrencyAmount > 0 && string.IsNullOrWhiteSpace(tier.CurrencyCode))
                || tier.Items.Any(item => item.Quantity <= 0 || item.ItemId == Guid.Empty)
                || (index > 0 && tier.FromRank <= ordered[index - 1].ToRank);

            if (invalid)
            {
                return ClanWarsErrors.RewardTableInvalid;
            }
        }

        var table = new RewardTable { Id = Guid.CreateVersion7(), Name = trimmed, CreatedAt = now };
        table._tiers.AddRange(ordered.Select(tier => new RewardTier(
            table.Id, tier.FromRank, tier.ToRank, tier.ClanPoints, tier.CurrencyCode, tier.CurrencyAmount, tier.Items, tier.MinBattles)));

        return table;
    }

    public RewardTier? TierFor(int rank) => _tiers.Find(tier => tier.Covers(rank));
}
