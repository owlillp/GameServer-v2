namespace Shared.IntegrationEvents.ClanWars.Events;

public sealed record SeasonStartedEvent(
    Guid SeasonId,
    int Number,
    string Name,
    DateTime StartsAt,
    DateTime EndsAt,
    DateTime StartedAt);

/// <param name="Top">Final standings (top clans) frozen when the season closed.</param>
public sealed record SeasonEndedEvent(
    Guid SeasonId,
    int Number,
    string Name,
    DateTime EndedAt,
    IReadOnlyList<SeasonStanding> Top);

public sealed record SeasonStanding(int Rank, Guid ClanId, long Points);
