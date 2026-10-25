namespace SessionService.Domain;

public static class SessionConstants
{
    public const int REGION_MAX_LENGTH = 32;
    public const int MODE_MAX_LENGTH = 32;
    public const int ADDRESS_MAX_LENGTH = 256;
    public const int MAX_MODES = 16;
    public const int MAX_CAPACITY = 1000;
    public const int ACTIVITY_ID_MAX_LENGTH = 64;
    public const int MAX_REPORT_PLAYERS = 100;
    public const int MAX_STATS_JSON_LENGTH = 64 * 1024;
    public const int FLAG_MAX_LENGTH = 64;
    public const int REASON_MAX_LENGTH = 64;

    public const string FREE_ROAM_KIND = "free-roam";
    public const string MATCH_KIND = "match";
}
