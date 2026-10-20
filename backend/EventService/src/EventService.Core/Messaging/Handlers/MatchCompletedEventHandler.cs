using EventService.Core.Abstractions;
using EventService.Domain.Events;
using EventService.Domain.Progress;
using Microsoft.Extensions.Logging;
using Shared.Core.Database;
using Shared.IntegrationEvents.Sessions.Events;

namespace EventService.Core.Messaging.Handlers;

public sealed class MatchCompletedEventHandler(
    ILogger<MatchCompletedEventHandler> logger,
    IEventRepository eventRepository,
    IEventParticipationRepository participationRepository,
    ITransactionManager transactionManager)
{
    public async Task Handle(MatchCompletedEvent @event, CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;
        IReadOnlyList<GameEvent> activeEvents = await eventRepository.ListActiveAsync(now, cancellationToken);

        foreach (GameEvent gameEvent in activeEvents)
        {
            await ProcessEventAsync(gameEvent, @event, now, cancellationToken);
        }

        if (activeEvents.Count > 0)
        {
            await transactionManager.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ProcessEventAsync(
        GameEvent gameEvent,
        MatchCompletedEvent @event,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (await participationRepository.MatchProcessedAsync(
                gameEvent.Id,
                @event.AccountId,
                @event.MatchId,
                cancellationToken))
        {
            return;
        }

        EventProgress progress = await participationRepository.GetOrCreateProgressAsync(
            gameEvent.Id,
            @event.AccountId,
            cancellationToken);

        foreach (EventQuest quest in gameEvent.Quests)
        {
            int amount = GetMetricAmount(quest.Metric, @event);
            if (amount <= 0)
            {
                continue;
            }

            DateTime periodStart = quest.GetPeriodStart(now, gameEvent.StartsAt);
            EventQuestProgress? questProgress = await participationRepository.FindQuestProgressAsync(
                quest.Id,
                @event.AccountId,
                periodStart,
                cancellationToken);

            if (questProgress is null)
            {
                var createResult = EventQuestProgress.Create(
                    gameEvent.Id,
                    quest.Id,
                    @event.AccountId,
                    periodStart);

                if (createResult.IsFailure)
                {
                    logger.LogWarning(
                        "Failed to create quest progress for event {EventId} and account {AccountId}: {Error}",
                        gameEvent.Id,
                        @event.AccountId,
                        createResult.Error.ToString());

                    continue;
                }

                questProgress = createResult.Value;
                await participationRepository.AddQuestProgressAsync(questProgress, cancellationToken);
            }

            bool completedNow = questProgress.Apply(amount, quest.Target);
            if (completedNow)
            {
                progress.AddPoints(quest.Points);

                logger.LogInformation(
                    "Quest {QuestCode} completed by account {AccountId} in event {EventCode}",
                    quest.Code,
                    @event.AccountId,
                    gameEvent.Code);
            }
        }

        var receiptResult = EventMatchReceipt.Create(gameEvent.Id, @event.AccountId, @event.MatchId);
        if (receiptResult.IsFailure)
        {
            logger.LogWarning(
                "Failed to create match receipt for event {EventId} and match {MatchId}",
                gameEvent.Id,
                @event.MatchId);

            return;
        }

        await participationRepository.AddMatchReceiptAsync(receiptResult.Value, cancellationToken);
    }

    private static int GetMetricAmount(EventQuestMetric metric, MatchCompletedEvent @event) =>
        metric switch
        {
            EventQuestMetric.MATCHES => 1,
            EventQuestMetric.WINS => @event.IsWin ? 1 : 0,
            EventQuestMetric.KILLS => Math.Max(0, @event.Kills),
            EventQuestMetric.SCORE => Math.Max(0, @event.Score),
            _ => 0,
        };
}
