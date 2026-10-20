namespace Shared.IntegrationEvents.Sessions;

public static class SessionEventRouting
{
    public const string EVENTS_EXCHANGE = "session-events";

    public static class RoutingKeys
    {
        public const string MATCH_COMPLETED = "match-completed.session";

        public const string ALL_SESSION_EVENTS = "*.session";
    }
}
