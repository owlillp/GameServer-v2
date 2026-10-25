using ClanWarsService.Contracts;
using ClanWarsService.Core.Abstractions;
using ClanWarsService.Core.Configuration;
using ClanWarsService.Core.Services;
using ClanWarsService.Domain;
using ClanWarsService.Domain.Seasons;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace ClanWarsService.Core.Features;

public sealed record CreateSeasonCommand(CreateSeasonRequest Request) : ICommand;

public sealed record UpdateSeasonCommand(Guid SeasonId, CreateSeasonRequest Request) : ICommand;

public sealed record PublishSeasonCommand(Guid SeasonId) : ICommand;

public sealed record AdjustScoreCommand(Guid AdminAccountId, Guid SeasonId, AdjustScoreRequest Request) : ICommand;

public sealed record RebuildLeaderboardCommand(Guid SeasonId) : ICommand;

public sealed record ListSeasonsQuery : IQuery;

public sealed record ListScoreReceiptsQuery(Guid SeasonId, Guid? ClanId, int Limit) : IQuery;

public sealed class CreateSeasonHandler(
    ISeasonRepository seasonRepository,
    ITransactionManager transactionManager,
    IOptionsMonitor<ClanWarsOptions> options,
    TimeProvider timeProvider) : ICommandHandler<SeasonDto, CreateSeasonCommand>
{
    public async Task<Result<SeasonDto, Error>> Handle(CreateSeasonCommand command, CancellationToken cancellationToken = default)
    {
        CreateSeasonRequest request = command.Request;
        int grace = request.FinalizationGraceMinutes ?? options.CurrentValue.DefaultFinalizationGraceMinutes;

        var seasonResult = Season.CreateDraft(
            await seasonRepository.NextNumberAsync(cancellationToken),
            request.Name,
            request.StartsAt,
            request.EndsAt,
            TimeSpan.FromMinutes(grace),
            request.ScoringRules.ToDomain(),
            request.RewardTableId,
            timeProvider.GetUtcNow().UtcDateTime);

        if (seasonResult.IsFailure)
        {
            return seasonResult.Error;
        }

        await seasonRepository.AddAsync(seasonResult.Value, cancellationToken);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? saveResult.Error : seasonResult.Value.ToDto();
    }
}

public sealed class UpdateSeasonHandler(
    ISeasonRepository seasonRepository,
    ITransactionManager transactionManager,
    IOptionsMonitor<ClanWarsOptions> options,
    TimeProvider timeProvider) : ICommandHandler<SeasonDto, UpdateSeasonCommand>
{
    public async Task<Result<SeasonDto, Error>> Handle(UpdateSeasonCommand command, CancellationToken cancellationToken = default)
    {
        Season? season = await seasonRepository.GetAsync(command.SeasonId, cancellationToken);
        if (season is null)
        {
            return ClanWarsErrors.SeasonNotFound;
        }

        CreateSeasonRequest request = command.Request;
        int grace = request.FinalizationGraceMinutes ?? options.CurrentValue.DefaultFinalizationGraceMinutes;

        var updateResult = season.UpdateDraft(
            request.Name,
            request.StartsAt,
            request.EndsAt,
            TimeSpan.FromMinutes(grace),
            request.ScoringRules.ToDomain(),
            request.RewardTableId,
            timeProvider.GetUtcNow().UtcDateTime);

        if (updateResult.IsFailure)
        {
            return updateResult.Error;
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? saveResult.Error : season.ToDto();
    }
}

/// <summary>
/// Publishing makes the schedule binding: phase triggers are created right after the commit
/// (and recreated by the startup reconcile if that step is lost).
/// </summary>
public sealed class PublishSeasonHandler(
    ISeasonRepository seasonRepository,
    ISeasonScheduler scheduler,
    ITransactionManager transactionManager,
    TimeProvider timeProvider) : ICommandHandler<SeasonDto, PublishSeasonCommand>
{
    public async Task<Result<SeasonDto, Error>> Handle(PublishSeasonCommand command, CancellationToken cancellationToken = default)
    {
        Season? season = await seasonRepository.GetAsync(command.SeasonId, cancellationToken);
        if (season is null)
        {
            return ClanWarsErrors.SeasonNotFound;
        }

        if (await seasonRepository.OverlapsAsync(season.Id, season.StartsAt, season.FinalizesAt, cancellationToken))
        {
            return ClanWarsErrors.SeasonOverlaps;
        }

        var publishResult = season.Publish(timeProvider.GetUtcNow().UtcDateTime);
        if (publishResult.IsFailure)
        {
            return publishResult.Error;
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        await scheduler.ScheduleAsync(season, cancellationToken);
        return season.ToDto();
    }
}

/// <summary>
/// Manual correction (tests before battles exist, disputes). The receipt records who and why;
/// the operation id makes a retried call harmless.
/// </summary>
public sealed class AdjustScoreHandler(ScoreService scoreService) : ICommandHandler<ScoreReceiptDto, AdjustScoreCommand>
{
    public async Task<Result<ScoreReceiptDto, Error>> Handle(AdjustScoreCommand command, CancellationToken cancellationToken = default)
    {
        AdjustScoreRequest request = command.Request;

        var result = await scoreService.ApplyAsync(
            new ScoreChange(
                command.SeasonId,
                request.ClanId,
                $"admin:{request.OperationId ?? Guid.CreateVersion7()}",
                request.Amount,
                request.Reason,
                command.AdminAccountId),
            cancellationToken);

        return result.IsFailure ? result.Error : result.Value.ToDto();
    }
}

public sealed class RebuildLeaderboardHandler(LeaderboardSync sync) : ICommandHandler<bool, RebuildLeaderboardCommand>
{
    public async Task<Result<bool, Error>> Handle(RebuildLeaderboardCommand command, CancellationToken cancellationToken = default) =>
        await sync.SyncAsync(command.SeasonId, force: true, cancellationToken);
}

public sealed class ListSeasonsHandler(ISeasonRepository seasonRepository) : IQueryHandler<IReadOnlyList<SeasonDto>, ListSeasonsQuery>
{
    public async Task<Result<IReadOnlyList<SeasonDto>, Error>> Handle(ListSeasonsQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Season> seasons = await seasonRepository.ListAsync(cancellationToken);
        return seasons.Select(season => season.ToDto()).ToList();
    }
}

public sealed class ListScoreReceiptsHandler(IScoreRepository scoreRepository)
    : IQueryHandler<IReadOnlyList<ScoreReceiptDto>, ListScoreReceiptsQuery>
{
    public async Task<Result<IReadOnlyList<ScoreReceiptDto>, Error>> Handle(ListScoreReceiptsQuery query, CancellationToken cancellationToken = default)
    {
        var receipts = await scoreRepository.ListReceiptsAsync(query.SeasonId, query.ClanId, Math.Clamp(query.Limit, 1, 500), cancellationToken);
        return receipts.Select(receipt => receipt.ToDto()).ToList();
    }
}
