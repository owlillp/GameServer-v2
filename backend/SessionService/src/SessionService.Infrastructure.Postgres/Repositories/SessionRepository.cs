using Microsoft.EntityFrameworkCore;
using SessionService.Core.Abstractions;
using SessionService.Domain.Sessions;

namespace SessionService.Infrastructure.Postgres.Repositories;

public sealed class SessionRepository(SessionServiceDbContext dbContext) : ISessionRepository
{
    public Task<Session?> GetAsync(Guid sessionId, CancellationToken cancellationToken) =>
        dbContext.Sessions.FirstOrDefaultAsync(session => session.Id == sessionId, cancellationToken);

    public Task<Session?> GetCurrentByPlayerAsync(Guid accountId, CancellationToken cancellationToken) =>
        OpenSessions()
            .Where(session => session.Players.Any(player => player.AccountId == accountId && player.LeftAt == null))
            .OrderByDescending(session => session.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Session>> ListJoinableFreeRoamAsync(
        string region,
        string mode,
        int limit,
        CancellationToken cancellationToken) =>
        await dbContext.Sessions
            .AsNoTracking()
            .Where(session => session.Kind == SessionKind.FreeRoam
                && session.Status == SessionStatus.Active
                && session.Region == region
                && session.Mode == mode
                && session.PlayerCount < session.MaxPlayers)
            .OrderByDescending(session => session.PlayerCount)
            .ThenBy(session => session.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, int>> SumReservedSlotsByServerAsync(
        IReadOnlyCollection<Guid> serverIds,
        CancellationToken cancellationToken)
    {
        Guid[] ids = [.. serverIds];

        return await OpenSessions()
            .Where(session => ids.Contains(session.ServerId))
            .GroupBy(session => session.ServerId)
            .Select(group => new { ServerId = group.Key, Reserved = group.Sum(session => session.MaxPlayers) })
            .ToDictionaryAsync(row => row.ServerId, row => row.Reserved, cancellationToken);
    }

    public async Task<IReadOnlyList<Session>> ClaimForLivenessCheckAsync(
        DateTime createdBefore,
        int limit,
        CancellationToken cancellationToken)
    {
        string allocating = nameof(SessionStatus.Allocating);
        string active = nameof(SessionStatus.Active);

        List<Guid> ids = await dbContext.Database
            .SqlQuery<Guid>($"""
                SELECT id AS "Value" FROM sessions.sessions
                WHERE status IN ({allocating}, {active}) AND created_at < {createdBefore}
                ORDER BY liveness_checked_at NULLS FIRST
                LIMIT {limit}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        if (ids.Count == 0)
        {
            return [];
        }

        return await dbContext.Sessions
            .Where(session => ids.Contains(session.Id))
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Session session, CancellationToken cancellationToken) =>
        dbContext.Sessions.AddAsync(session, cancellationToken).AsTask();

    private IQueryable<Session> OpenSessions() =>
        dbContext.Sessions.Where(session =>
            session.Status == SessionStatus.Allocating || session.Status == SessionStatus.Active);
}
