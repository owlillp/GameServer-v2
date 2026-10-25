using System.Text.Json;

namespace Shared.IntegrationEvents.Realtime;

public sealed record RealtimeNotification(
    Guid Id,
    string Type,
    string Audience,
    DateTime OccurredAt,
    int Version,
    JsonElement Payload);

public static class RealtimeNotificationTypes
{
    public const string EVENT_STARTED = "event.started";
    public const string EVENT_ENDED = "event.ended";
    public const string EVENT_REWARD_GRANTED = "event.reward.granted";
    public const string WALLET_CREDITED = "wallet.credited";
    public const string WALLET_DEBITED = "wallet.debited";
    public const string INVENTORY_ITEMS_GRANTED = "inventory.items.granted";
    public const string MATCH_READY = "matchmaking.match.ready";
    public const string CLAN_MEMBER_JOINED = "clan.member.joined";
    public const string CLAN_MEMBER_LEFT = "clan.member.left";
    public const string CLAN_MEMBER_ROLE_CHANGED = "clan.member.role_changed";
    public const string CLAN_DISBANDED = "clan.disbanded";
    public const string CLANWARS_SEASON_STARTED = "clanwars.season.started";
    public const string CLANWARS_SEASON_ENDED = "clanwars.season.ended";
}

public static class RealtimeAudiences
{
    public const string LIVE_OPS = "liveops";

    public static string Player(Guid accountId) => $"player:{accountId}";

    public static string Clan(Guid clanId) => $"clan:{clanId}";
}
