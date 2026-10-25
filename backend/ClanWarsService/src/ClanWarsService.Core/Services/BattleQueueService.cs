using ClanWarsService.Contracts;
using ClanWarsService.Core.Abstractions;
using ClanWarsService.Core.Configuration;
using ClanWarsService.Domain;
using ClanWarsService.Domain.Battles;
using ClanWarsService.Domain.Roster;
using ClanWarsService.Domain.Seasons;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace ClanWarsService.Core.Services;

/// <summary>
/// Officers put a squad up for a battle. Checks run on the roster projection: the actor is the
/// leader or an officer, every squad member is in the clan, the window is open, the clan is not
/// already queued or fighting. One queued squad per clan is also a unique index.
/// </summary>
public sealed class BattleQueueService(
    ISeasonRepository seasonRepository,
    IRosterRepository rosterRepository,
    IBattleRepository battleRepository,
    IScoreRepository scoreRepository,
    ITransactionManager transactionManager,
    IOptionsMonitor<ClanWarsOptions> options,
    TimeProvider timeProvider)
{
    private static readonly string[] MANAGER_ROLES = ["leader", "officer"];

    public async Task<Result<BattleQueueEntry, Error>> QueueAsync(
        Guid accountId,
        IReadOnlyList<Guid> squad,
        CancellationToken cancellationToken)
    {
        var actorResult = await GetManagerAsync(accountId, cancellationToken);
        if (actorResult.IsFailure)
        {
            return actorResult.Error;
        }

        Guid clanId = actorResult.Value.ClanId;
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        Season? season = await seasonRepository.GetCurrentAsync(cancellationToken);
        if (season is null || season.Status != SeasonStatus.Active)
        {
            return ClanWarsErrors.SeasonNotScoring;
        }

        if (!await battleRepository.IsWindowOpenAsync(season.Id, now, cancellationToken))
        {
            return ClanWarsErrors.BattleWindowClosed;
        }

        IReadOnlyList<RosterEntry> roster = await rosterRepository.ListEntriesAsync(clanId, cancellationToken);
        HashSet<Guid> members = [.. roster.Select(entry => entry.AccountId)];
        if (!squad.All(members.Contains))
        {
            return ClanWarsErrors.SquadNotInClan;
        }

        DateTime joinedBefore = now.AddHours(-options.CurrentValue.MinSquadTenureHours);
        if (roster.Any(entry => squad.Contains(entry.AccountId) && entry.JoinedAt > joinedBefore))
        {
            return ClanWarsErrors.SquadMemberTooNew;
        }

        if (await battleRepository.GetQueuedAsync(clanId, cancellationToken) is not null)
        {
            return ClanWarsErrors.ClanAlreadyQueued;
        }

        if (await battleRepository.HasOpenBattleAsync(clanId, cancellationToken))
        {
            return ClanWarsErrors.ClanInBattle;
        }

        long rating = (await scoreRepository.GetAsync(season.Id, clanId, cancellationToken))?.Points ?? 0;
        ClanWarsOptions current = options.CurrentValue;

        var entryResult = BattleQueueEntry.Create(season.Id, clanId, squad, current.MinSquadSize, current.MaxSquadSize, rating, accountId, now);
        if (entryResult.IsFailure)
        {
            return entryResult.Error;
        }

        await battleRepository.AddQueueEntryAsync(entryResult.Value, cancellationToken);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error.IsUniqueViolation() ? ClanWarsErrors.ClanAlreadyQueued : saveResult.Error;
        }

        return entryResult.Value;
    }

    public async Task<UnitResult<Error>> LeaveAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var actorResult = await GetManagerAsync(accountId, cancellationToken);
        if (actorResult.IsFailure)
        {
            return actorResult.Error;
        }

        BattleQueueEntry? entry = await battleRepository.GetQueuedAsync(actorResult.Value.ClanId, cancellationToken);
        if (entry is null)
        {
            return ClanWarsErrors.NotQueued;
        }

        entry.Cancel(timeProvider.GetUtcNow().UtcDateTime);
        return await transactionManager.SaveChangesAsync(cancellationToken);
    }

    public async Task<Result<MyBattlesDto, Error>> GetMineAsync(Guid accountId, CancellationToken cancellationToken)
    {
        RosterEntry? entry = await rosterRepository.GetEntryAsync(accountId, cancellationToken);
        if (entry is null)
        {
            return ClanWarsErrors.NotInClan;
        }

        BattleQueueEntry? queued = await battleRepository.GetQueuedAsync(entry.ClanId, cancellationToken);
        IReadOnlyList<Battle> battles = await battleRepository.ListByClanAsync(entry.ClanId, 20, cancellationToken);

        return new MyBattlesDto(queued?.ToDto(), [.. battles.Select(battle => battle.ToDto())]);
    }

    private async Task<Result<RosterEntry, Error>> GetManagerAsync(Guid accountId, CancellationToken cancellationToken)
    {
        RosterEntry? entry = await rosterRepository.GetEntryAsync(accountId, cancellationToken);
        if (entry is null)
        {
            return ClanWarsErrors.NotInClan;
        }

        return MANAGER_ROLES.Contains(entry.Role, StringComparer.OrdinalIgnoreCase) ? entry : ClanWarsErrors.NotOfficer;
    }
}

public static class BattleMapper
{
    public static BattleQueueEntryDto ToDto(this BattleQueueEntry entry) =>
        new(entry.Id, entry.SeasonId, entry.ClanId, entry.Squad, entry.Rating, entry.Status.ToString().ToUpperInvariant(), entry.BattleId, entry.QueuedAt);

    public static BattleDto ToDto(this Battle battle) =>
        new(
            battle.Id,
            battle.SeasonId,
            battle.ClanAId,
            battle.ClanBId,
            battle.SquadA,
            battle.SquadB,
            battle.Status.ToString().ToUpperInvariant(),
            battle.MatchId,
            battle.WinnerClanId,
            battle.PointsA,
            battle.PointsB,
            battle.CancelReason,
            battle.CreatedAt,
            battle.StartedAt,
            battle.FinishedAt);

    public static BattleWindowDto ToDto(this BattleWindow window, DateTime now) =>
        new(window.StartsAt, window.EndsAt, window.IsOpen(now));
}
