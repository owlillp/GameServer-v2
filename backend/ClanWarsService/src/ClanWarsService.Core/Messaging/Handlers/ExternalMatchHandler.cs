using ClanWarsService.Core.Abstractions;
using ClanWarsService.Core.Services;
using ClanWarsService.Domain.Battles;
using Shared.IntegrationEvents.Matchmaking.Events;

namespace ClanWarsService.Core.Messaging.Handlers;

/// <summary>
/// Matchmaking reports back on the matches it plays for clan battles (external ref = battle).
/// </summary>
public sealed class ExternalMatchHandler(
    IBattleRepository battleRepository,
    BattleResultService resultService,
    TimeProvider timeProvider)
{
    public async Task Handle(ExternalMatchStartedEvent @event, CancellationToken cancellationToken)
    {
        if (Battle.TryParseExternalRef(@event.ExternalRef, out Guid battleId))
        {
            await battleRepository.TryStartAsync(battleId, @event.MatchId, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        }
    }

    public async Task Handle(ExternalMatchCompletedEvent @event, CancellationToken cancellationToken)
    {
        if (Battle.TryParseExternalRef(@event.ExternalRef, out Guid battleId))
        {
            long scoreA = @event.Teams.FirstOrDefault(team => team.Side == 1)?.Score ?? -1;
            long scoreB = @event.Teams.FirstOrDefault(team => team.Side == 2)?.Score ?? -1;

            await resultService.ApplyResultAsync(battleId, @event.MatchId, @event.WinnerSide, cancellationToken, scoreA, scoreB);
        }
    }

    public async Task Handle(ExternalMatchCancelledEvent @event, CancellationToken cancellationToken)
    {
        if (Battle.TryParseExternalRef(@event.ExternalRef, out Guid battleId))
        {
            await resultService.CancelAsync(battleId, $"match-{@event.Reason}", cancellationToken);
        }
    }
}
