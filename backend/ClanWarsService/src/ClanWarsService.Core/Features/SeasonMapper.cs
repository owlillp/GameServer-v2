using ClanWarsService.Contracts;
using ClanWarsService.Domain.Scores;
using ClanWarsService.Domain.Seasons;

namespace ClanWarsService.Core.Features;

public static class SeasonMapper
{
    public static SeasonDto ToDto(this Season season) =>
        new(
            season.Id,
            season.Number,
            season.Name,
            season.Status.ToString().ToUpperInvariant(),
            season.StartsAt,
            season.EndsAt,
            season.FinalizesAt,
            new ScoringRulesDto(season.ScoringRules.WinPoints, season.ScoringRules.DrawPoints, season.ScoringRules.LossPoints),
            season.RewardTableId,
            season.PublishedAt,
            season.StartedAt,
            season.EndedAt,
            season.ClosedAt);

    public static ScoreReceiptDto ToDto(this ScoreReceipt receipt) =>
        new(receipt.Id, receipt.SeasonId, receipt.ClanId, receipt.SourceId, receipt.Amount, receipt.Reason, receipt.ActorId, receipt.CreatedAt);

    public static ScoringRules? ToDomain(this ScoringRulesDto? rules) =>
        rules is null ? null : new ScoringRules(rules.WinPoints, rules.DrawPoints, rules.LossPoints);
}
