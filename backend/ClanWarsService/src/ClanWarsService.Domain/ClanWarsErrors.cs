using Shared.SharedKernel.Errors;

namespace ClanWarsService.Domain;

public static class ClanWarsErrors
{
    public static Error SeasonNameInvalid { get; } =
        Error.Validation("clanwars.season.name.invalid", "Season name must be 1-64 characters", "name");

    public static Error SeasonScheduleInvalid { get; } =
        Error.Validation("clanwars.season.schedule.invalid", "Season must end after it starts", "endsAt");

    public static Error SeasonInPast { get; } =
        Error.Validation("clanwars.season.schedule.past", "Season cannot end in the past", "endsAt");

    public static Error SeasonOverlaps { get; } =
        Error.Conflict("clanwars.season.overlaps", "Season overlaps another published season", "startsAt");

    public static Error SeasonNotFound { get; } =
        Error.NotFound("clanwars.season.not_found", "Season was not found", "seasonId");

    public static Error SeasonNotDraft { get; } =
        Error.Conflict("clanwars.season.not_draft", "Only a draft season can be changed or published", "seasonId");

    public static Error SeasonNotScoring { get; } =
        Error.Conflict("clanwars.season.not_scoring", "The season does not accept scores now", "seasonId");

    public static Error NoCurrentSeason { get; } =
        Error.NotFound("clanwars.season.none", "There is no current or upcoming season", "seasonId");

    public static Error ScoringRulesInvalid { get; } =
        Error.Validation("clanwars.season.rules.invalid", "Scoring rules must not be negative", "scoringRules");

    public static Error ScoreInvalid { get; } =
        Error.Validation("clanwars.score.invalid", "Score amount must not be zero and the reason is required", "amount");

    public static Error ScoreAlreadyApplied { get; } =
        Error.Conflict("clanwars.score.duplicate", "This score source has already been applied", "sourceId");

    public static Error ClanNotFound { get; } =
        Error.NotFound("clanwars.clan.not_found", "Clan is unknown or disbanded", "clanId");

    public static Error NotInClan { get; } =
        Error.NotFound("clanwars.clan.not_member", "Player is not in a clan", "accountId");

    public static Error ClanNotRanked { get; } =
        Error.NotFound("clanwars.clan.not_ranked", "Clan has no score in this season", "clanId");
}
