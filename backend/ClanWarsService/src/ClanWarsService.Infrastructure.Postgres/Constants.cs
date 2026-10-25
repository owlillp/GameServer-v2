namespace ClanWarsService.Infrastructure.Postgres;

public static class Constants
{
    public const string EF_MIGRATION_HISTORY = "__EFMigrationsHistory";
    public const string SCHEMA = "clanwars";

    /// <summary>
    /// Shadow property mapped by Npgsql to the <c>xmin</c> system column.
    /// </summary>
    public const string ROW_VERSION = "xmin";
}
