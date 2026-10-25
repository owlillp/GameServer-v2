using System.Data.Common;
using ClanWarsService.Core.Abstractions;
using ClanWarsService.Core.Configuration;
using ClanWarsService.Core.Services;
using ClanWarsService.Domain.Scores;
using ClanWarsService.Domain.Seasons;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;
using Xunit;

namespace ClanWarsService.UnitTests;

public sealed class SeasonLifecycleTests
{
    private static readonly DateTime CREATED = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task SeasonMissedWhileOffline_StartsAndCloses_PublishingEachEventOnce()
    {
        Season season = Season.CreateDraft(1, "Test", CREATED.AddMinutes(5), CREATED.AddHours(1), TimeSpan.FromMinutes(5), null, null, CREATED).Value;
        season.Publish(CREATED);

        Guid winner = Guid.NewGuid();
        var scores = new FakeScores([
            new SeasonScore(season.Id, Guid.NewGuid(), 10, 1, 0, CREATED),
            new SeasonScore(season.Id, winner, 50, 2, 2, CREATED),
        ]);
        var publisher = new RecordingPublisher();
        var clock = new FixedClock(CREATED.AddHours(5));
        SeasonLifecycle lifecycle = Create(season, scores, publisher, clock);

        await lifecycle.AdvanceAsync(season.Id, CancellationToken.None);
        await lifecycle.AdvanceAsync(season.Id, CancellationToken.None);

        Assert.Equal(SeasonStatus.Closed, season.Status);
        Assert.Equal(1, publisher.Started);
        Assert.Single(publisher.Ended);
        Assert.Equal(winner, publisher.Ended[0][0].ClanId);
        Assert.Equal([1, 2], scores.Results.Select(result => result.Rank));
    }

    [Fact]
    public async Task BoundaryFiredEarly_ChangesNothing()
    {
        Season season = Season.CreateDraft(1, "Test", CREATED.AddMinutes(5), CREATED.AddHours(1), TimeSpan.Zero, null, null, CREATED).Value;
        season.Publish(CREATED);
        var publisher = new RecordingPublisher();

        await Create(season, new FakeScores([]), publisher, new FixedClock(CREATED.AddMinutes(1))).AdvanceAsync(season.Id, CancellationToken.None);

        Assert.Equal(SeasonStatus.Scheduled, season.Status);
        Assert.Equal(0, publisher.Started);
    }

    private static SeasonLifecycle Create(Season season, FakeScores scores, RecordingPublisher publisher, TimeProvider clock) =>
        new(
            new FakeSeasons(season),
            scores,
            publisher,
            new NoTransaction(),
            new StaticOptions(),
            clock,
            NullLogger<SeasonLifecycle>.Instance);

    private sealed class FixedClock(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(now);
    }

    private sealed class StaticOptions : IOptionsMonitor<ClanWarsOptions>
    {
        public ClanWarsOptions CurrentValue { get; } = new();

        public ClanWarsOptions Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<ClanWarsOptions, string?> listener) => null;
    }

    private sealed class RecordingPublisher : IClanWarsEventPublisher
    {
        public int Started { get; private set; }

        public List<IReadOnlyList<SeasonResult>> Ended { get; } = [];

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
    }

    private sealed class FakeSeasons(Season stored) : ISeasonRepository
    {
        public Task<Season?> GetAsync(Guid seasonId, CancellationToken cancellationToken) => Task.FromResult<Season?>(stored);

        public Task<Season?> GetForUpdateAsync(Guid seasonId, CancellationToken cancellationToken) => Task.FromResult<Season?>(stored);

        public Task<IReadOnlyList<Season>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Season>>([stored]);

        public Task<IReadOnlyList<Season>> ListOpenAsync(CancellationToken cancellationToken) => ListAsync(cancellationToken);

        public Task<Season?> GetCurrentAsync(CancellationToken cancellationToken) => GetAsync(stored.Id, cancellationToken);

        public Task<int> NextNumberAsync(CancellationToken cancellationToken) => Task.FromResult(2);

        public Task<bool> OverlapsAsync(Guid seasonId, DateTime startsAt, DateTime finalizesAt, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task AddAsync(Season season, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeScores(IReadOnlyList<SeasonScore> scores) : IScoreRepository
    {
        public List<SeasonResult> Results { get; } = [];

        public Task<IReadOnlyList<SeasonScore>> ListTopAsync(Guid seasonId, int offset, int limit, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SeasonScore>>([.. scores.OrderByDescending(score => score.Points).Skip(offset).Take(limit)]);

        public Task AddResultsAsync(IReadOnlyCollection<SeasonResult> results, CancellationToken cancellationToken)
        {
            Results.AddRange(results);
            return Task.CompletedTask;
        }

        public Task AddReceiptAsync(ScoreReceipt receipt, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<ScoreReceipt>> ListReceiptsAsync(Guid seasonId, Guid? clanId, int limit, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task ApplyAsync(Guid seasonId, Guid clanId, long delta, int battles, int wins, DateTime now, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<SeasonScore?> GetAsync(Guid seasonId, Guid clanId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<SeasonScore>> ListByClansAsync(Guid seasonId, IReadOnlyCollection<Guid> clanIds, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<SeasonScore>> ListAfterAsync(Guid seasonId, Guid? afterClanId, int limit, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<int> CountAsync(Guid seasonId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<int> CountAheadOfAsync(SeasonScore score, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<MemberSeasonStats>> ListMemberStatsAsync(Guid seasonId, Guid clanId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IAsyncDisposable?> TryAcquireRebuildLockAsync(Guid seasonId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class NoTransaction : ITransactionManager
    {
        public Task<UnitResult<Error>> BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.FromResult(UnitResult.Success<Error>());

        public Task<UnitResult<Error>> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(UnitResult.Success<Error>());

        public Task<UnitResult<Error>> CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.FromResult(UnitResult.Success<Error>());

        public DbConnection GetDbConnection() => throw new NotSupportedException();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
