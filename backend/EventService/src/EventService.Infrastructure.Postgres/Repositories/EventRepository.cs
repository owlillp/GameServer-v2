using CSharpFunctionalExtensions;
using EventService.Core.Abstractions;
using EventService.Domain;
using EventService.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Shared.SharedKernel.Errors;

namespace EventService.Infrastructure.Postgres.Repositories;

public sealed class EventRepository(EventServiceDbContext dbContext) : IEventRepository
{
    public async Task<Result<GameEvent, Error>> GetByIdAsync(
        Guid eventId,
        bool includeDetails,
        CancellationToken cancellationToken)
    {
        IQueryable<GameEvent> query = dbContext.Events;

        if (includeDetails)
        {
            query = query
                .Include(gameEvent => gameEvent.Quests)
                .Include(gameEvent => gameEvent.Rewards)
                .AsSplitQuery();
        }

        GameEvent? gameEvent = await query.FirstOrDefaultAsync(entity => entity.Id == eventId, cancellationToken);

        return gameEvent is not null ? gameEvent : EventErrors.EventNotFound;
    }

    public async Task<Result<GameEvent, Error>> GetByCodeAsync(
        string code,
        bool includeDetails,
        CancellationToken cancellationToken)
    {
        string normalizedCode = code.Trim().ToLowerInvariant();
        IQueryable<GameEvent> query = dbContext.Events;

        if (includeDetails)
        {
            query = query
                .Include(gameEvent => gameEvent.Quests)
                .Include(gameEvent => gameEvent.Rewards)
                .AsSplitQuery();
        }

        GameEvent? gameEvent = await query.FirstOrDefaultAsync(
            entity => entity.Code == normalizedCode,
            cancellationToken);

        return gameEvent is not null ? gameEvent : EventErrors.EventNotFound;
    }

    public async Task<IReadOnlyList<GameEvent>> ListAsync(
        bool includeDetails,
        CancellationToken cancellationToken)
    {
        IQueryable<GameEvent> query = dbContext.Events;

        if (includeDetails)
        {
            query = query
                .Include(gameEvent => gameEvent.Quests)
                .Include(gameEvent => gameEvent.Rewards)
                .AsSplitQuery();
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GameEvent>> ListActiveAsync(
        DateTime now,
        CancellationToken cancellationToken) =>
        await dbContext.Events
            .Include(gameEvent => gameEvent.Quests)
            .Where(gameEvent => gameEvent.Status == EventStatus.PUBLISHED)
            .Where(gameEvent => gameEvent.StartsAt <= now && gameEvent.EndsAt > now)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<GameEvent>> ListPendingNotificationsAsync(
        DateTime now,
        CancellationToken cancellationToken) =>
        await dbContext.Events
            .Where(gameEvent => gameEvent.Status == EventStatus.PUBLISHED)
            .Where(gameEvent =>
                (gameEvent.StartedNotifiedAt == null && gameEvent.StartsAt <= now)
                || (gameEvent.EndedNotifiedAt == null && gameEvent.GraceEndsAt <= now))
            .ToListAsync(cancellationToken);

    public Task<bool> CodeExistsAsync(string code, Guid? excludeEventId, CancellationToken cancellationToken)
    {
        string normalizedCode = code.Trim().ToLowerInvariant();

        return dbContext.Events.AnyAsync(
            gameEvent => gameEvent.Code == normalizedCode
                && (excludeEventId == null || gameEvent.Id != excludeEventId),
            cancellationToken);
    }

    public Task AddAsync(GameEvent gameEvent, CancellationToken cancellationToken)
    {
        dbContext.Events.Add(gameEvent);
        return Task.CompletedTask;
    }
}
