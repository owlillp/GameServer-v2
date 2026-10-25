namespace MatchmakingService.Domain;

public static class MatchmakingConstants
{
    public const int MIN_PARTY_SIZE = 2;
    public const int MAX_PARTY_SIZE = 5;
    public const int PARTY_CODE_LENGTH = 8;

    public const int MODE_MAX_LENGTH = 32;
    public const int REGION_MAX_LENGTH = 32;
    public const int ADDRESS_MAX_LENGTH = 256;
    public const string DEFAULT_REGION = "eu-west";
    public const int EXTERNAL_REF_MAX_LENGTH = 128;
    public const int MAX_EXTERNAL_TEAM_SIZE = 10;
    public const int CURRENCY_MAX_LENGTH = 16;
    public const int ITEM_CODE_MAX_LENGTH = 64;
    public const int ITEM_KIND_MAX_LENGTH = 32;
    public const int SLOT_MAX_LENGTH = 32;
    public const int FAILURE_REASON_MAX_LENGTH = 512;

    public const int MAX_REWARD_ITEMS = 20;
    public const int MAX_MATCH_RESULTS = 50;
    public const int MAX_LEADERBOARD_LIMIT = 200;

    public const int DEFAULT_MATCH_DURATION_HOURS = 48;
    public const int MAX_MATCH_DURATION_HOURS = 24 * 30;
}
