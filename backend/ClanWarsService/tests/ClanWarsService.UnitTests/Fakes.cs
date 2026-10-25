using System.Data.Common;
using ClanWarsService.Core.Abstractions;
using ClanWarsService.Core.Configuration;
using ClanWarsService.Domain.Battles;
using ClanWarsService.Domain.Rewards;
using ClanWarsService.Domain.Roster;
using ClanWarsService.Domain.Scores;
using ClanWarsService.Domain.Seasons;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace ClanWarsService.UnitTests;

internal sealed class FixedClock(DateTime now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(now);
}

internal sealed class StaticOptions : IOptionsMonitor<ClanWarsOptions>
{
    public ClanWarsOptions CurrentValue { get; } = new();

    public ClanWarsOptions Get(string? name) => CurrentValue;

    public IDisposable? OnChange(Action<ClanWarsOptions, string?> listener) => null;
}

internal sealed class RecordingPublisher : IClanWarsEventPublisher
{
    public int Started { get; private set; }

    public List<IReadOnlyList<SeasonResult>> Ended { get; } = [];

    public List<(Guid BattleId, string Status)> BattlesFinished { get; } = [];

    public Task PublishSeasonStartedAsync(Season season)
    {
        Started++;
        return Task.CompletedTask;
    }

    public Task PublishSeasonEndedAsync(Season season, IReadOnlyList<SeasonResult> top)
    {
        Ended.Add(top);
        return Task.CompletedTask;
    }

    public Task PublishBattleCreatedAsync(Battle battle, string mode, string region) => Task.CompletedTask;

    public List<AbuseSignal> Signals { get; } = [];

    public Task PublishAbuseSignalAsync(AbuseSignal signal)
    {
        Signals.Add(signal);
        return Task.CompletedTask;
    }

    public Task PublishClanPointsGrantedAsync(SeasonRewardGrant grant, int seasonNumber, DateTime now) => Task.CompletedTask;

    public Task PublishGrantItemsAsync(SeasonRewardGrant grant, DateTime now) => Task.CompletedTask;

    public Task PublishBattleFinishedAsync(Battle battle, string status, BattleOutcome? outcome, DateTime finishedAt)
    {
        BattlesFinished.Add((battle.Id, status));
        return Task.CompletedTask;
    }
}

internal sealed class FakeSeasons(Season stored) : ISeasonRepository
{
    public Task<Season?> GetAsync(Guid seasonId, CancellationToken cancellationToken) => Task.FromResult<Season?>(stored);

    public Task<Season?> GetForUpdateAsync(Guid seasonId, CancellationToken cancellationToken) => Task.FromResult<Season?>(stored);

    public Task<IReadOnlyList<Season>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Season>>([stored]);

    public Task<IReadOnlyList<Season>> ListOpenAsync(CancellationToken cancellationToken) => ListAsync(cancellationToken);

    public Task<Season?> GetCurrentAsync(CancellationToken cancellationToken) => GetAsync(stored.Id, cancellationToken);

    public Task<int> NextNumberAsync(CancellationToken cancellationToken) => Task.FromResult(2);

    public Task<IReadOnlyList<Season>> ListByStatusAsync(SeasonStatus status, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Season>>(stored.Status == status ? [stored] : []);

    public Task<bool> OverlapsAsync(Guid seasonId, DateTime startsAt, DateTime finalizesAt, CancellationToken cancellationToken) => Task.FromResult(false);

    public Task AddAsync(Season season, CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// In-memory scores: receipts, points and member stats, enough to observe double counting.
/// </summary>
internal sealed class FakeScores(IReadOnlyList<SeasonScore> initial, IReadOnlyList<MemberSeasonStats>? members = null) : IScoreRepository
{
    public Task<IReadOnlyList<MemberSeasonStats>> ListMemberStatsBySeasonAsync(Guid seasonId, int minBattles, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MemberSeasonStats>>([.. (members ?? []).Where(member => member.Battles >= minBattles)]);

    public List<SeasonResult> Results { get; } = [];

    public List<ScoreReceipt> Receipts { get; } = [];

    public Dictionary<Guid, long> Points { get; } = [];

    public Dictionary<Guid, int> MemberBattles { get; } = [];

    public Task<IReadOnlyList<SeasonScore>> ListTopAsync(Guid seasonId, int offset, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SeasonScore>>([.. initial.OrderByDescending(score => score.Points).Skip(offset).Take(limit)]);

    public Task AddResultsAsync(IReadOnlyCollection<SeasonResult> results, CancellationToken cancellationToken)
    {
        Results.AddRange(results);
        return Task.CompletedTask;
    }

    public Task AddReceiptAsync(ScoreReceipt receipt, CancellationToken cancellationToken)
    {
        Receipts.Add(receipt);
        return Task.CompletedTask;
    }

    public Task<bool> ReceiptExistsAsync(Guid seasonId, Guid clanId, string sourceId, CancellationToken cancellationToken) =>
        Task.FromResult(Receipts.Exists(receipt => receipt.ClanId == clanId && string.Equals(receipt.SourceId, sourceId, StringComparison.Ordinal)));

    public Task ApplyAsync(Guid seasonId, Guid clanId, long delta, int battles, int wins, DateTime now, CancellationToken cancellationToken)
    {
        Points[clanId] = Points.GetValueOrDefault(clanId) + delta;
        return Task.CompletedTask;
    }

    public Task ApplyMemberAsync(Guid seasonId, Guid accountId, Guid clanId, long points, int battles, int wins, CancellationToken cancellationToken)
    {
        MemberBattles[accountId] = MemberBattles.GetValueOrDefault(accountId) + battles;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ScoreReceipt>> ListReceiptsAsync(Guid seasonId, Guid? clanId, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ScoreReceipt>>(Receipts);

    public Task<SeasonScore?> GetAsync(Guid seasonId, Guid clanId, CancellationToken cancellationToken) => Task.FromResult<SeasonScore?>(null);

    public Task<IReadOnlyList<SeasonScore>> ListByClansAsync(Guid seasonId, IReadOnlyCollection<Guid> clanIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SeasonScore>>([]);

    public Task<IReadOnlyList<SeasonScore>> ListAfterAsync(Guid seasonId, Guid? afterClanId, int limit, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<int> CountAsync(Guid seasonId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<int> CountAheadOfAsync(SeasonScore score, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IReadOnlyList<MemberSeasonStats>> ListMemberStatsAsync(Guid seasonId, Guid clanId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IAsyncDisposable?> TryAcquireRebuildLockAsync(Guid seasonId, CancellationToken cancellationToken) => throw new NotSupportedException();
}

/// <summary>
/// In-memory battles with the same conditional transitions as the SQL repository.
/// </summary>
internal sealed class FakeBattles : IBattleRepository
{
    public Dictionary<Guid, (Battle Battle, BattleStatus Status)> Battles { get; } = [];

    public void Add(Battle battle) => Battles[battle.Id] = (battle, battle.Status);

    public BattleStatus StatusOf(Guid battleId) => Battles[battleId].Status;

    public Task<Battle?> GetBattleAsync(Guid battleId, CancellationToken cancellationToken) =>
        Task.FromResult(Battles.TryGetValue(battleId, out var entry) ? WithStatus(entry.Battle, entry.Status) : null);

    public Task<bool> TryStartAsync(Guid battleId, Guid matchId, DateTime now, CancellationToken cancellationToken) =>
        Task.FromResult(Move(battleId, BattleStatus.InProgress, BattleStatus.Scheduled));

    public Task<bool> TryFinishAsync(Guid battleId, Guid? matchId, BattleOutcome outcome, DateTime now, CancellationToken cancellationToken) =>
        Task.FromResult(Move(battleId, BattleStatus.Finished, BattleStatus.Scheduled, BattleStatus.InProgress));

    public Task<bool> TryCancelAsync(Guid battleId, string reason, DateTime now, CancellationToken cancellationToken) =>
        Task.FromResult(Move(battleId, BattleStatus.Cancelled, BattleStatus.Scheduled, BattleStatus.InProgress));

    public Task<IReadOnlyList<Guid>> ListOpenAsync(Guid? seasonId, DateTime? createdBefore, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Guid>>([.. Battles.Where(pair => pair.Value.Status is BattleStatus.Scheduled or BattleStatus.InProgress).Select(pair => pair.Key)]);

    public Task<int> CancelQueuedAsync(Guid seasonId, DateTime now, CancellationToken cancellationToken) => Task.FromResult(0);

    public Task AddWindowsAsync(IReadOnlyCollection<BattleWindow> windows, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<IReadOnlyList<BattleWindow>> ListWindowsAsync(Guid seasonId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<BattleWindow>>([]);

    public Task<bool> IsWindowOpenAsync(Guid seasonId, DateTime now, CancellationToken cancellationToken) => Task.FromResult(true);

    public Task<BattleQueueEntry?> GetQueuedAsync(Guid clanId, CancellationToken cancellationToken) => Task.FromResult<BattleQueueEntry?>(null);

    public Task AddQueueEntryAsync(BattleQueueEntry entry, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<IReadOnlyList<BattleQueueEntry>> ClaimQueuedAsync(Guid seasonId, int limit, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<BattleQueueEntry>>([]);

    public Task AddBattleAsync(Battle battle, CancellationToken cancellationToken)
    {
        Add(battle);
        return Task.CompletedTask;
    }

    public Task<bool> HasOpenBattleAsync(Guid clanId, CancellationToken cancellationToken) => Task.FromResult(false);

    public List<AbuseSignal> Signals { get; } = [];

    public Task<int> CountFinishedBetweenAsync(Guid seasonId, Guid clanA, Guid clanB, CancellationToken cancellationToken) =>
        Task.FromResult(Battles.Values.Count(entry => entry.Status == BattleStatus.Finished));

    public Task AddSignalsAsync(IReadOnlyCollection<AbuseSignal> signals, CancellationToken cancellationToken)
    {
        Signals.AddRange(signals);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AbuseSignal>> ListSignalsAsync(Guid? seasonId, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AbuseSignal>>(Signals);

    public Task<int> CountBattlesBetweenSinceAsync(Guid clanA, Guid clanB, DateTime since, CancellationToken cancellationToken) => Task.FromResult(0);

    public Task<IReadOnlyList<Battle>> ListByClanAsync(Guid clanId, int limit, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Battle>>([]);

    private bool Move(Guid battleId, BattleStatus target, params BattleStatus[] from)
    {
        if (!Battles.TryGetValue(battleId, out var entry) || !from.Contains(entry.Status))
        {
            return false;
        }

        Battles[battleId] = (entry.Battle, target);
        return true;
    }

    // The stored entity keeps its creation status; the fake tracks the current one beside it.
    private static Battle WithStatus(Battle battle, BattleStatus status)
    {
        typeof(Battle).GetProperty(nameof(Battle.Status))!.SetValue(battle, status);
        return battle;
    }
}

internal sealed class NoopLeaderboard : ISeasonLeaderboardStore
{
    public Task SetAsync(Guid seasonId, IReadOnlyCollection<SeasonScore> scores, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ReplaceAsync(Guid seasonId, Func<Guid?, CancellationToken, Task<IReadOnlyList<SeasonScore>>> loadBatch, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<IReadOnlyList<LeaderboardRow>> GetRangeAsync(Guid seasonId, int offset, int limit, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<LeaderboardRow>>([]);

    public Task<long?> GetRankAsync(Guid seasonId, Guid clanId, CancellationToken cancellationToken) => Task.FromResult<long?>(null);

    public Task<long> CountAsync(Guid seasonId, CancellationToken cancellationToken) => Task.FromResult(0L);
}

internal sealed class NoRoster : IRosterRepository
{
    public Task<ClanProjection?> GetClanAsync(Guid clanId, CancellationToken cancellationToken) => Task.FromResult<ClanProjection?>(null);

    public Task<IReadOnlyList<ClanProjection>> ListClansAsync(IReadOnlyCollection<Guid> clanIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ClanProjection>>([]);

    public Task AddClanAsync(ClanProjection clan, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<RosterEntry?> GetEntryAsync(Guid accountId, CancellationToken cancellationToken) => Task.FromResult<RosterEntry?>(null);

    public Task<IReadOnlyList<RosterEntry>> ListEntriesAsync(Guid clanId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RosterEntry>>([]);

    public Task AddEntryAsync(RosterEntry entry, CancellationToken cancellationToken) => Task.CompletedTask;

    public void RemoveEntry(RosterEntry entry)
    {
        // Not used by the tests.
    }
}

internal sealed class NoTransaction : ITransactionManager
{
    public Task<UnitResult<Error>> BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.FromResult(UnitResult.Success<Error>());

    public Task<UnitResult<Error>> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(UnitResult.Success<Error>());

    public Task<UnitResult<Error>> CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.FromResult(UnitResult.Success<Error>());

    public DbConnection GetDbConnection() => throw new NotSupportedException();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class FakeRewards : IRewardRepository
{
    public Dictionary<Guid, RewardTable> Tables { get; } = [];

    public List<SeasonRewardGrant> Grants { get; } = [];

    public Task AddTableAsync(RewardTable table, CancellationToken cancellationToken)
    {
        Tables[table.Id] = table;
        return Task.CompletedTask;
    }

    public Task<RewardTable?> GetTableAsync(Guid tableId, CancellationToken cancellationToken) => Task.FromResult(Tables.GetValueOrDefault(tableId));

    public Task<IReadOnlyList<RewardTable>> ListTablesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RewardTable>>([.. Tables.Values]);

    public Task AddGrantsAsync(IReadOnlyCollection<SeasonRewardGrant> grants, CancellationToken cancellationToken)
    {
        Grants.AddRange(grants);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SeasonRewardGrant>> ClaimDueAsync(DateTime now, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SeasonRewardGrant>>([.. Grants.Where(grant => grant.Status == RewardGrantStatus.Pending && grant.NextAttemptAt <= now).Take(limit)]);

    public Task<SeasonRewardGrant?> GetGrantAsync(Guid grantId, CancellationToken cancellationToken) => Task.FromResult(Grants.Find(grant => grant.Id == grantId));

    public Task<IReadOnlyList<SeasonRewardGrant>> ListAsync(Guid seasonId, RewardGrantStatus? status, Guid? accountId, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SeasonRewardGrant>>(Grants);

    public Task<RewardSummary> SummarizeAsync(Guid seasonId, CancellationToken cancellationToken) =>
        Task.FromResult(new RewardSummary(
            Grants.Count(grant => grant.Status == RewardGrantStatus.Pending),
            Grants.Count(grant => grant.Status == RewardGrantStatus.Granted),
            Grants.Count(grant => grant.Status == RewardGrantStatus.Failed)));

    public Task<IReadOnlyList<SeasonRewardGrant>> ListFailedAsync(Guid seasonId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SeasonRewardGrant>>([.. Grants.Where(grant => grant.Status == RewardGrantStatus.Failed)]);

    public Task<IReadOnlyList<SeasonRewardGrant>> ListCreditedSinceAsync(DateTime since, Guid? afterId, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SeasonRewardGrant>>([]);

    public Task<IReadOnlyList<SeasonRewardGrant>> ListStuckItemsAsync(DateTime publishedBefore, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SeasonRewardGrant>>([]);
}
