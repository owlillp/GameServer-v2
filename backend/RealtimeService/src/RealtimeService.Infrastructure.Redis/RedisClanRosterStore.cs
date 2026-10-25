using RealtimeService.Core.Abstractions;
using StackExchange.Redis;

namespace RealtimeService.Infrastructure.Redis;

public sealed class RedisClanRosterStore(IConnectionMultiplexer redis) : IClanRosterStore
{
    private const string CLAN_PREFIX = "realtime:clan-member:";
    private const string CONNECTIONS_PREFIX = "realtime:connections:";

    // Connections of a crashed node are never removed explicitly; the set expires instead.
    private static readonly TimeSpan CONNECTIONS_TTL = TimeSpan.FromDays(1);

    private const string REMOVE_IF_EQUAL = """
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            return redis.call('DEL', KEYS[1])
        end
        return 0
        """;

    private IDatabase Database => redis.GetDatabase();

    public Task SetClanAsync(Guid accountId, Guid clanId, CancellationToken cancellationToken) =>
        Database.StringSetAsync(ClanKey(accountId), clanId.ToString("D"));

    public Task RemoveClanAsync(Guid accountId, Guid clanId, CancellationToken cancellationToken) =>
        Database.ScriptEvaluateAsync(REMOVE_IF_EQUAL, [ClanKey(accountId)], [clanId.ToString("D")]);

    public async Task<Guid?> GetClanAsync(Guid accountId, CancellationToken cancellationToken)
    {
        RedisValue value = await Database.StringGetAsync(ClanKey(accountId));
        return Guid.TryParse(value.ToString(), out Guid clanId) ? clanId : null;
    }

    public async Task AddConnectionAsync(Guid accountId, string connectionId, CancellationToken cancellationToken)
    {
        RedisKey key = ConnectionsKey(accountId);
        await Database.SetAddAsync(key, connectionId);
        await Database.KeyExpireAsync(key, CONNECTIONS_TTL);
    }

    public Task RemoveConnectionAsync(Guid accountId, string connectionId, CancellationToken cancellationToken) =>
        Database.SetRemoveAsync(ConnectionsKey(accountId), connectionId);

    public async Task<IReadOnlyList<string>> GetConnectionsAsync(Guid accountId, CancellationToken cancellationToken)
    {
        RedisValue[] members = await Database.SetMembersAsync(ConnectionsKey(accountId));
        return [.. members.Select(member => member.ToString())];
    }

    private static RedisKey ClanKey(Guid accountId) => $"{CLAN_PREFIX}{accountId:D}";

    private static RedisKey ConnectionsKey(Guid accountId) => $"{CONNECTIONS_PREFIX}{accountId:D}";
}
