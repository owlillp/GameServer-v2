using ClanWarsService.Core.Abstractions;
using ClanWarsService.Domain;
using ClanWarsService.Domain.Rewards;
using ClanWarsService.Domain.Scores;
using ClanWarsService.Domain.Seasons;

namespace ClanWarsService.Core.Services;

/// <summary>
/// Turns the frozen standings into reward grants, inside the closing transaction: one grant with
/// clan points per rewarded clan and one per member who fought enough battles for that clan.
/// Members are taken from battle contributions, not from the current roster, so a player who
/// fought and then left still gets the reward.
/// </summary>
public sealed class RewardPlanner(
    IRewardRepository rewardRepository,
    IScoreRepository scoreRepository)
{
    private const int INSERT_BATCH = 1000;

    public async Task<int> PlanAsync(
        Season season,
        IReadOnlyList<SeasonResult> results,
        DateTime now,
        Func<CancellationToken, Task> saveBatch,
        CancellationToken cancellationToken)
    {
        RewardTable table = await rewardRepository.GetTableAsync(season.RewardTableId!.Value, cancellationToken)
            ?? throw ClanWarsErrors.RewardTableNotFound.ToException();

        IReadOnlyList<MemberSeasonStats> stats = await scoreRepository.ListMemberStatsBySeasonAsync(season.Id, 0, cancellationToken);
        ILookup<Guid, MemberSeasonStats> membersByClan = stats.ToLookup(member => member.ClanId);

        var batch = new List<SeasonRewardGrant>(INSERT_BATCH);
        int total = 0;

        foreach (SeasonResult result in results)
        {
            RewardTier? tier = table.TierFor(result.Rank);
            if (tier is null)
            {
                continue;
            }

            if (tier.ClanPoints > 0)
            {
                batch.Add(SeasonRewardGrant.ForClan(season.Id, result.ClanId, result.Rank, tier.ClanPoints, now));
            }

            if (tier.HasMemberReward)
            {
                batch.AddRange(membersByClan[result.ClanId]
                    .Where(member => member.Battles >= tier.MinBattles)
                    .Select(member => SeasonRewardGrant.ForMember(season.Id, result.ClanId, member.AccountId, result.Rank, tier, now)));
            }

            if (batch.Count >= INSERT_BATCH)
            {
                total += await FlushAsync(batch, saveBatch, cancellationToken);
            }
        }

        total += await FlushAsync(batch, saveBatch, cancellationToken);
        return total;
    }

    private async Task<int> FlushAsync(List<SeasonRewardGrant> batch, Func<CancellationToken, Task> saveBatch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
        {
            return 0;
        }

        int count = batch.Count;
        await rewardRepository.AddGrantsAsync([.. batch], cancellationToken);
        await saveBatch(cancellationToken);
        batch.Clear();

        return count;
    }
}
