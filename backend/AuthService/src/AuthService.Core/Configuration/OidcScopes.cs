namespace AuthService.Core.Configuration;

public static class OidcScopes
{
    public const string OPEN_ID = "openid";
    public const string PROFILE = "profile";
    public const string EMAIL = "email";
    public const string OFFLINE_ACCESS = "offline_access";

    public const string AUTH = "auth";
    public const string CLANS = "clans";

    public static IReadOnlyList<string> All { get; } =
    [
        OPEN_ID,
        PROFILE,
        EMAIL,
        OFFLINE_ACCESS,
        AUTH,
        CLANS,
    ];

    public static class Resources
    {
        public const string AUTH_RESOURCE = "auth-service";
        public const string CLANS_RESOURCE = "clan-service";
    }
}