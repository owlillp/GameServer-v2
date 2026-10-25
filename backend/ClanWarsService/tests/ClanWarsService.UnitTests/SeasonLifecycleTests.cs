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
            new FakeBattles(),
            publisher,
            new NoTransaction(),
            new StaticOptions(),
            clock,
            NullLogger<SeasonLifecycle>.Instance);
}
