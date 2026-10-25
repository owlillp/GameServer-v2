using System.Text.Json;
using Shared.IntegrationEvents.Sessions.Events;
using Xunit;

namespace MatchmakingService.UnitTests;

/// <summary>
/// match-completed.session moved from Matchmaking to SessionService and gained optional fields.
/// Consumers built against the old contract (EventService, ProfileService) must keep reading it,
/// and the new contract must read messages still produced by the old publisher.
/// </summary>
public sealed class MatchCompletedContractTests
{
    private static readonly DateTime COMPLETED_AT = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    public static TheoryData<string> SerializerProfiles => new() { "default", "web" };

    [Theory]
    [MemberData(nameof(SerializerProfiles))]
    public void NewMessage_IsReadableByOldConsumer(string profile)
    {
        JsonSerializerOptions options = Options(profile);
        var message = new MatchCompletedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "RANKED",
            IsWin: true,
            Kills: 7,
            Score: 150,
            COMPLETED_AT,
            SessionId: Guid.NewGuid(),
            ExternalRef: "battle-1",
            ReportPlayerCount: 4);

        LegacyMatchCompletedEvent? legacy = JsonSerializer.Deserialize<LegacyMatchCompletedEvent>(
            JsonSerializer.Serialize(message, options),
            options);

        Assert.NotNull(legacy);
        Assert.Equal(message.MatchId, legacy.MatchId);
        Assert.Equal(message.AccountId, legacy.AccountId);
        Assert.Equal(message.Mode, legacy.Mode);
        Assert.Equal(message.IsWin, legacy.IsWin);
        Assert.Equal(message.Kills, legacy.Kills);
        Assert.Equal(message.Score, legacy.Score);
        Assert.Equal(message.CompletedAt, legacy.CompletedAt);
    }

    [Theory]
    [MemberData(nameof(SerializerProfiles))]
    public void OldMessage_IsReadableByNewConsumer(string profile)
    {
        JsonSerializerOptions options = Options(profile);
        var legacy = new LegacyMatchCompletedEvent(Guid.NewGuid(), Guid.NewGuid(), "RANKED", false, 3, 80, COMPLETED_AT);

        MatchCompletedEvent? message = JsonSerializer.Deserialize<MatchCompletedEvent>(
            JsonSerializer.Serialize(legacy, options),
            options);

        Assert.NotNull(message);
        Assert.Equal(legacy.MatchId, message.MatchId);
        Assert.Equal(legacy.Score, message.Score);
        Assert.Null(message.SessionId);
        Assert.Null(message.ExternalRef);
        Assert.Null(message.ReportPlayerCount);
    }

    [Fact]
    public void LegacyFields_KeepTheirNamesAndOrder()
    {
        string[] legacy = [.. typeof(LegacyMatchCompletedEvent).GetConstructors()[0].GetParameters().Select(p => p.Name!)];
        string[] current = [.. typeof(MatchCompletedEvent).GetConstructors()[0].GetParameters().Select(p => p.Name!)];

        Assert.Equal(legacy, current.Take(legacy.Length), StringComparer.Ordinal);
        Assert.All(
            typeof(MatchCompletedEvent).GetConstructors()[0].GetParameters().Skip(legacy.Length),
            parameter => Assert.True(parameter.HasDefaultValue, $"{parameter.Name} must be optional"));
    }

    private static JsonSerializerOptions Options(string profile) =>
        string.Equals(profile, "web", StringComparison.Ordinal)
            ? new JsonSerializerOptions(JsonSerializerDefaults.Web)
            : new JsonSerializerOptions();

    /// <summary>
    /// The contract as it was before SessionService (2026-10-21).
    /// </summary>
    private sealed record LegacyMatchCompletedEvent(
        Guid MatchId,
        Guid AccountId,
        string Mode,
        bool IsWin,
        int Kills,
        int Score,
        DateTime CompletedAt);
}
