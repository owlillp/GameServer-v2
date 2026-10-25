using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace ClanWarsService.Domain.Seasons;

public sealed class Season
{
    public const int NAME_MAX_LENGTH = 64;

    public Guid Id { get; private set; }

    public int Number { get; private set; }

    public string Name { get; private set; } = null!;

    public DateTime StartsAt { get; private set; }

    public DateTime EndsAt { get; private set; }

    /// <summary>
    /// Battles that started before <see cref="EndsAt"/> may still report results until this moment.
    /// </summary>
    public DateTime FinalizesAt { get; private set; }

    public SeasonStatus Status { get; private set; }

    public ScoringRules ScoringRules { get; private set; } = ScoringRules.Default;

    public Guid? RewardTableId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? PublishedAt { get; private set; }

    public DateTime? StartedAt { get; private set; }

    public DateTime? EndedAt { get; private set; }

    public DateTime? ClosedAt { get; private set; }

    // EF Core
    private Season() { }

    public static Result<Season, Error> CreateDraft(
        int number,
        string name,
        DateTime startsAt,
        DateTime endsAt,
        TimeSpan finalizationGrace,
        ScoringRules? rules,
        Guid? rewardTableId,
        DateTime now)
    {
        var season = new Season
        {
            Id = Guid.CreateVersion7(),
            Number = number,
            Status = SeasonStatus.Draft,
            CreatedAt = now,
        };

        var updateResult = season.UpdateDraft(name, startsAt, endsAt, finalizationGrace, rules, rewardTableId, now);
        return updateResult.IsFailure ? updateResult.Error : season;
    }

    public UnitResult<Error> UpdateDraft(
        string name,
        DateTime startsAt,
        DateTime endsAt,
        TimeSpan finalizationGrace,
        ScoringRules? rules,
        Guid? rewardTableId,
        DateTime now)
    {
        if (Status != SeasonStatus.Draft)
        {
            return ClanWarsErrors.SeasonNotDraft;
        }

        string trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length is 0 or > NAME_MAX_LENGTH)
        {
            return ClanWarsErrors.SeasonNameInvalid;
        }

        if (endsAt <= startsAt || finalizationGrace < TimeSpan.Zero)
        {
            return ClanWarsErrors.SeasonScheduleInvalid;
        }

        if (endsAt <= now)
        {
            return ClanWarsErrors.SeasonInPast;
        }

        ScoringRules scoring = rules ?? ScoringRules.Default;
        if (!scoring.IsValid)
        {
            return ClanWarsErrors.ScoringRulesInvalid;
        }

        Name = trimmed;
        StartsAt = DateTime.SpecifyKind(startsAt, DateTimeKind.Utc);
        EndsAt = DateTime.SpecifyKind(endsAt, DateTimeKind.Utc);
        FinalizesAt = EndsAt.Add(finalizationGrace);
        ScoringRules = scoring;
        RewardTableId = rewardTableId;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Publish(DateTime now)
    {
        if (Status != SeasonStatus.Draft)
        {
            return ClanWarsErrors.SeasonNotDraft;
        }

        if (EndsAt <= now)
        {
            return ClanWarsErrors.SeasonInPast;
        }

        Status = SeasonStatus.Scheduled;
        PublishedAt = now;

        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// The phase the schedule says at <paramref name="now"/>. Rewarding and Closed are reached only
    /// through <see cref="Advance"/>, because closing needs the final results snapshot.
    /// </summary>
    public SeasonStatus ScheduledPhase(DateTime now)
    {
        if (Status is SeasonStatus.Draft or SeasonStatus.Rewarding or SeasonStatus.Closed)
        {
            return Status;
        }

        if (now < StartsAt)
        {
            return SeasonStatus.Scheduled;
        }

        return now < EndsAt ? SeasonStatus.Active : SeasonStatus.Finalizing;
    }

    /// <summary>
    /// True after the finalization grace: results can be frozen and the season closed.
    /// </summary>
    public bool IsReadyToClose(DateTime now) => Status == SeasonStatus.Finalizing && now >= FinalizesAt;

    /// <summary>
    /// Scores are accepted while the season is running and during finalization
    /// (battles that began before the end).
    /// </summary>
    public bool AcceptsScores => Status is SeasonStatus.Active or SeasonStatus.Finalizing;

    /// <summary>
    /// Applies every transition the schedule allows, in order, and returns them.
    /// Running it again changes nothing, so a repeated or late job is harmless.
    /// </summary>
    public IReadOnlyList<SeasonStatus> Advance(DateTime now)
    {
        var applied = new List<SeasonStatus>();
        SeasonStatus target = ScheduledPhase(now);

        if (Status == SeasonStatus.Scheduled && target is SeasonStatus.Active or SeasonStatus.Finalizing)
        {
            Status = SeasonStatus.Active;
            StartedAt = now;
            applied.Add(SeasonStatus.Active);
        }

        if (Status == SeasonStatus.Active && target == SeasonStatus.Finalizing)
        {
            Status = SeasonStatus.Finalizing;
            EndedAt = now;
            applied.Add(SeasonStatus.Finalizing);
        }

        return applied;
    }

    /// <summary>
    /// After the results snapshot. With a reward table the season waits in Rewarding for the
    /// grants (stage 7); without one it closes at once.
    /// </summary>
    public SeasonStatus Close(DateTime now)
    {
        if (Status != SeasonStatus.Finalizing)
        {
            return Status;
        }

        if (RewardTableId is null)
        {
            Status = SeasonStatus.Closed;
            ClosedAt = now;
        }
        else
        {
            Status = SeasonStatus.Rewarding;
        }

        return Status;
    }
}
