namespace Shared.IntegrationEvents.Matchmaking;

public static class MatchmakingEventRouting
{
    public const string COMMANDS_EXCHANGE = "matchmaking-commands";
    public const string EVENTS_EXCHANGE = "matchmaking-events";

    public static class RoutingKeys
    {
        public const string GRANT_REWARD = "grant-reward.matchmaking";
        public const string ALLOCATE_MATCH = "allocate-match.matchmaking";

        public const string REWARD_GRANTED = "reward-granted.matchmaking";
        public const string REWARD_FAILED = "reward-failed.matchmaking";
        public const string MATCH_READY = "match-ready.matchmaking";

        public const string ALL_COMMANDS = "*.matchmaking";
        public const string ALL_EVENTS = "*.matchmaking";
    }
}
