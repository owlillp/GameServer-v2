namespace Shared.IntegrationEvents.Players;

public static class PlayerEventRouting
{
    public const string EXCHANGE = "player-events";

    public static class RoutingKeys
    {
        public const string PROFILE_UPDATED = "updated.profile";
    }
}
