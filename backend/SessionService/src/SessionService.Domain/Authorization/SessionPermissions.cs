namespace SessionService.Domain.Authorization;

public static class SessionPermissions
{
    public static class Sessions
    {
        public const string VIEW = "sessions.view";
        public const string MANAGE = "sessions.manage";
    }

    public static IReadOnlyList<string> All { get; } =
    [
        Sessions.VIEW,
        Sessions.MANAGE,
    ];
}
