namespace Shared.IntegrationEvents.ClanWars;

public static class ClanWarsEventRouting
{
    public const string EVENTS_EXCHANGE = "clanwars-events";

    public static class RoutingKeys
    {
        public const string CLAN_POINTS_GRANTED = "clan-points-granted.clanwars";
    }
}
