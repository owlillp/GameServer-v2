namespace Shared.IntegrationEvents.Clans;

public static class ClanEventRouting
{
    public const string EVENTS_EXCHANGE = "clan-events";

    public static class RoutingKeys
    {
        public const string CLAN_CREATED = "clan-created.clan";
        public const string CLAN_DISBANDED = "clan-disbanded.clan";
        public const string MEMBER_JOINED = "member-joined.clan";
        public const string MEMBER_LEFT = "member-left.clan";
        public const string MEMBER_ROLE_CHANGED = "member-role-changed.clan";

        public const string ALL_CLAN_EVENTS = "*.clan";
    }
}
