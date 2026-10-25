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
}

public static class RealtimeAudiences
{
    public const string LIVE_OPS = "liveops";

    public static string Player(Guid accountId) => $"player:{accountId}";
}
