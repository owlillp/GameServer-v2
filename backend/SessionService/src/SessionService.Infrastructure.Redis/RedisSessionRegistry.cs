using SessionService.Core.Abstractions;
using StackExchange.Redis;

namespace SessionService.Infrastructure.Redis;

/// <summary>
/// <c>session:{id}</c> and <c>presence:server:{id}</c> live only while heartbeats keep extending them.
/// </summary>
public sealed class RedisSessionRegistry(IConnectionMultiplexer connection) : ISessionRegistry
{
    private const string SESSION_PREFIX = "session:";
    private const string SERVER_PREFIX = "presence:server:";
    private const string TICKET_PREFIX = "session:ticket:";

    private IDatabase Database => connection.GetDatabase();

    public Task TouchServerAsync(Guid serverId, TimeSpan ttl, CancellationToken cancellationToken) =>
        Database.StringSetAsync(ServerKey(serverId), 1, ttl);

    public Task<IReadOnlySet<Guid>> GetAliveServersAsync(
        IReadOnlyCollection<Guid> serverIds,
        CancellationToken cancellationToken) =>
        GetExistingAsync(serverIds, ServerKey);

    public async Task TouchSessionsAsync(
        IReadOnlyCollection<Guid> sessionIds,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        if (sessionIds.Count == 0)
        {
            return;
        }

        IBatch batch = Database.CreateBatch();
        Task[] writes = [.. sessionIds.Select(sessionId => batch.StringSetAsync(SessionKey(sessionId), 1, ttl))];
        batch.Execute();

        await Task.WhenAll(writes);
    }

    public Task<IReadOnlySet<Guid>> GetAliveAsync(
        IReadOnlyCollection<Guid> sessionIds,
        CancellationToken cancellationToken) =>
        GetExistingAsync(sessionIds, SessionKey);

    public Task RemoveSessionAsync(Guid sessionId, CancellationToken cancellationToken) =>
        Database.KeyDeleteAsync(SessionKey(sessionId));

    public Task<bool> TryUseTicketAsync(string ticketId, TimeSpan ttl, CancellationToken cancellationToken) =>
        Database.StringSetAsync($"{TICKET_PREFIX}{ticketId}", 1, ttl, When.NotExists);

    private async Task<IReadOnlySet<Guid>> GetExistingAsync(
        IReadOnlyCollection<Guid> ids,
        Func<Guid, RedisKey> keyOf)
    {
        if (ids.Count == 0)
        {
            return new HashSet<Guid>();
        }

        IBatch batch = Database.CreateBatch();
        (Guid Id, Task<bool> Exists)[] checks = [.. ids.Select(id => (id, batch.KeyExistsAsync(keyOf(id))))];
        batch.Execute();

        await Task.WhenAll(checks.Select(check => check.Exists));

        return checks
            .Where(check => check.Exists.Result)
            .Select(check => check.Id)
            .ToHashSet();
    }

    private static RedisKey SessionKey(Guid sessionId) => $"{SESSION_PREFIX}{sessionId}";

    private static RedisKey ServerKey(Guid serverId) => $"{SERVER_PREFIX}{serverId}";
}
