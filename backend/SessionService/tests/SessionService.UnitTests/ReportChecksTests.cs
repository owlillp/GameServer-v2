using SessionService.Domain;
using SessionService.Domain.Reports;
using SessionService.Domain.Sessions;
using Xunit;

namespace SessionService.UnitTests;

public sealed class ReportChecksTests
{
    private static readonly AntiCheatLimits LIMITS = new(
        MinMatchSeconds: 30,
        MaxMatchMinutes: 180,
        MaxScorePerMinute: 1000,
        MaxKillsPerMinute: 30);

    [Fact]
    public void Validate_AcceptsPlayersWhoLeftBeforeTheReport()
    {
        (Session session, Guid first, Guid second) = SessionWithTwoPlayers();
        session.Leave(second, TestData.NOW);

        MatchReport report = Report(TimeSpan.FromMinutes(5), Player(first, 100), Player(second, 50));

        Assert.True(ReportChecks.Validate(report, session).IsSuccess);
    }

    [Fact]
    public void Validate_RejectsPlayerWhoWasNeverInSession()
    {
        (Session session, Guid first, _) = SessionWithTwoPlayers();

        MatchReport report = Report(TimeSpan.FromMinutes(5), Player(first, 100), Player(Guid.NewGuid(), 50));

        var result = ReportChecks.Validate(report, session);
        Assert.Equal(SessionErrors.ReportPlayerUnknown, result.Error);
    }

    [Fact]
    public void Validate_RejectsDuplicatePlayersAndNegativeValues()
    {
        (Session session, Guid first, _) = SessionWithTwoPlayers();

        Assert.True(ReportChecks.Validate(Report(TimeSpan.FromMinutes(5), Player(first, 1), Player(first, 2)), session).IsFailure);
        Assert.True(ReportChecks.Validate(Report(TimeSpan.FromMinutes(5), Player(first, -1)), session).IsFailure);
    }

    [Fact]
    public void Flags_PlausibleReportHasNoFlags()
    {
        MatchReport report = Report(TimeSpan.FromMinutes(10), Player(Guid.NewGuid(), 5000, kills: 50));

        Assert.Empty(ReportChecks.Flags(report, LIMITS));
    }

    [Fact]
    public void Flags_TooShortAndTooFast()
    {
        Guid cheater = Guid.NewGuid();
        MatchReport report = Report(TimeSpan.FromSeconds(10), Player(cheater, 900, kills: 20));

        IReadOnlyList<string> flags = ReportChecks.Flags(report, LIMITS);

        Assert.Contains(ReportChecks.FLAG_TOO_SHORT, flags, StringComparer.Ordinal);
        Assert.Contains($"{ReportChecks.FLAG_SCORE_RATE}:{cheater}", flags, StringComparer.Ordinal);
        Assert.Contains($"{ReportChecks.FLAG_KILL_RATE}:{cheater}", flags, StringComparer.Ordinal);
    }

    [Fact]
    public void RawReport_IsSuspiciousWhenFlagged()
    {
        var clean = new RawReport(Guid.NewGuid(), Guid.NewGuid(), "a1", Guid.NewGuid(), "ranked", "{}", [], TestData.NOW);
        var flagged = new RawReport(Guid.NewGuid(), Guid.NewGuid(), "a2", Guid.NewGuid(), "ranked", "{}", ["x"], TestData.NOW);

        Assert.False(clean.IsSuspicious);
        Assert.True(flagged.IsSuspicious);
        Assert.Equal("RANKED", clean.Mode);
    }

    private static (Session Session, Guid First, Guid Second) SessionWithTwoPlayers()
    {
        Session session = TestData.FreeRoam(TestData.Server(), maxPlayers: 4);
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        session.Join(first, TestData.NOW);
        session.Join(second, TestData.NOW);

        return (session, first, second);
    }

    private static ReportedPlayer Player(Guid accountId, int score, int kills = 0) =>
        new(accountId, Team: 1, score, kills, Deaths: 0, IsWinner: false);

    private static MatchReport Report(TimeSpan duration, params ReportedPlayer[] players) =>
        new("activity-1", "ranked", TestData.NOW, TestData.NOW.Add(duration), players, StatsJson: null);
}
