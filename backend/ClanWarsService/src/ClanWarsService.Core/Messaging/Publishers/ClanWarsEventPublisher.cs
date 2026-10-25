using ClanWarsService.Core.Abstractions;
using ClanWarsService.Domain.Battles;
using ClanWarsService.Domain.Rewards;
using ClanWarsService.Domain.Scores;
using ClanWarsService.Domain.Seasons;
using Shared.IntegrationEvents.ClanWars.Commands;
using Shared.IntegrationEvents.ClanWars.Events;
using Shared.IntegrationEvents.Matchmaking.Commands;

namespace ClanWarsService.Core.Messaging.Publishers;

public sealed class ClanWarsEventPublisher(IOutboxService outbox) : IClanWarsEventPublisher
{
    public Task PublishSeasonStartedAsync(Season season) =>
        outbox.PublishAsync(new SeasonStartedEvent(
            season.Id,
            season.Number,
            season.Name,
            season.StartsAt,
            season.EndsAt,
            season.StartedAt ?? season.StartsAt));

    public Task PublishSeasonEndedAsync(Season season, IReadOnlyList<SeasonResult> top) =>
        outbox.PublishAsync(new SeasonEndedEvent(
            season.Id,
            season.Number,
            season.Name,
            season.EndedAt ?? season.EndsAt,
            [.. top.Select(result => new SeasonStanding(result.Rank, result.ClanId, result.Points))]));

    /// <summary>
    /// The match command (Matchmaking plays the battle on a game server) and the push to both clans.
    /// </summary>
    public async Task PublishBattleCreatedAsync(Battle battle, string mode, string region)
    {
        await outbox.PublishAsync(new CreateExternalMatchCommand(
            battle.ExternalRef,
            mode,
            region,
            [new ExternalMatchTeam(1, battle.SquadA), new ExternalMatchTeam(2, battle.SquadB)],
            battle.CreatedAt));

        await outbox.PublishAsync(new BattleFoundEvent(
            battle.Id,
            battle.SeasonId,
            battle.ClanAId,
            battle.ClanBId,
            battle.SquadA,
            battle.SquadB,
            battle.CreatedAt));
    }

    public Task PublishBattleFinishedAsync(Battle battle, string status, BattleOutcome? outcome, DateTime finishedAt) =>
        outbox.PublishAsync(new BattleFinishedEvent(
            battle.Id,
            battle.SeasonId,
            battle.ClanAId,
            battle.ClanBId,
            status,
            outcome?.WinnerClanId,
            outcome?.PointsA ?? 0,
            outcome?.PointsB ?? 0,
            finishedAt));

    /// <summary>
    /// ClanService applies it to its points ledger once per operation id (= grant id).
    /// </summary>
    public Task PublishClanPointsGrantedAsync(SeasonRewardGrant grant, int seasonNumber, DateTime now) =>
        outbox.PublishAsync(new ClanPointsGrantedEvent(
            grant.Id,
            grant.ClanId,
            grant.ClanPoints,
            $"clanwars:season:{seasonNumber}:rank:{grant.Rank}",
            now));

    public Task PublishGrantItemsAsync(SeasonRewardGrant grant, DateTime now) =>
        outbox.PublishAsync(new GrantSeasonRewardItemsCommand(
            grant.Id,
            grant.SeasonId,
            grant.AccountId!.Value,
            [.. grant.Items.Select(item => new GrantSeasonRewardItem(item.ItemId, item.Code, item.Kind, item.Slot, item.Quantity))],
            now));
}
