namespace Shared.IntegrationEvents.ClanWars;

public static class ClanWarsEventRouting
{
    public const string EVENTS_EXCHANGE = "clanwars-events";
    public const string COMMANDS_EXCHANGE = "clanwars-commands";

    public static class RoutingKeys
    {
        public const string CLAN_POINTS_GRANTED = "clan-points-granted.clanwars";
        public const string SEASON_STARTED = "season-started.clanwars";
        public const string SEASON_ENDED = "season-ended.clanwars";
        public const string BATTLE_FOUND = "battle-found.clanwars";
        public const string BATTLE_FINISHED = "battle-finished.clanwars";

        public const string GRANT_ITEMS = "grant-items.clanwars";
        public const string REWARD_ITEMS_GRANTED = "reward-items-granted.clanwars";
        public const string REWARD_ITEMS_FAILED = "reward-items-failed.clanwars";
    }
}
