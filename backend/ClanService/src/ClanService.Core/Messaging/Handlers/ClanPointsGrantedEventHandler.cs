using ClanService.Core.Services;
using ClanService.Domain;
using Microsoft.Extensions.Logging;
using Shared.Core.Database;
using Shared.IntegrationEvents.ClanWars.Events;

namespace ClanService.Core.Messaging.Handlers;

public sealed class ClanPointsGrantedEventHandler(
    ClanPointsService pointsService,
    ILogger<ClanPointsGrantedEventHandler> logger)
{
    public async Task Handle(ClanPointsGrantedEvent @event, CancellationToken cancellationToken)
    {
        var result = await pointsService.ApplyAsync(
            @event.ClanId,
            @event.OperationId,
            @event.Amount,
            @event.Reason,
            actorId: null,
            comment: null,
            cancellationToken);

        if (result.IsSuccess || result.Error == ClanErrors.PointsAlreadyApplied)
        {
            return;
        }

        if (result.Error == ClanErrors.ClanNotFound)
        {
            logger.LogWarning("Clan points {OperationId} for unknown clan {ClanId}", @event.OperationId, @event.ClanId);
            return;
        }

        throw result.Error.ToHandlerException();
    }
}
