namespace SessionService.Domain.Sessions;

public enum SessionKind
{
    FreeRoam = 1,
    Match = 2,
}

public static class SessionKindExtensions
{
    public static string ToWireName(this SessionKind kind) => kind switch
    {
        SessionKind.FreeRoam => SessionConstants.FREE_ROAM_KIND,
        SessionKind.Match => SessionConstants.MATCH_KIND,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown session kind"),
    };
}
