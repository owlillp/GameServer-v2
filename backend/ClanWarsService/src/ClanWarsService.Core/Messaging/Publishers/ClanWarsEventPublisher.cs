using ClanWarsService.Core.Abstractions;
using ClanWarsService.Domain.Scores;
using ClanWarsService.Domain.Seasons;
using Shared.IntegrationEvents.ClanWars.Events;

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
}
