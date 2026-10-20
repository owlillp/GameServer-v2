namespace Shared.IntegrationEvents.Events;

public static class EventRouting
{
    public const string COMMANDS_EXCHANGE = "event-commands";
    public const string EVENTS_EXCHANGE = "event-events";

    public static class RoutingKeys
    {
        public const string GRANT_REWARD = "grant-reward.event";

        public const string STARTED = "started.event";
        public const string ENDED = "ended.event";
        public const string REWARD_GRANTED = "reward-granted.event";
        public const string REWARD_FAILED = "reward-failed.event";

        public const string ALL_COMMANDS = "*.event";
        public const string ALL_EVENTS = "*.event";
    }
}
