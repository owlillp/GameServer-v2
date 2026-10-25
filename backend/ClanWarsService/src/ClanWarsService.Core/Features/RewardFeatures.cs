using ClanWarsService.Contracts;
using ClanWarsService.Core.Abstractions;
using ClanWarsService.Domain;
using ClanWarsService.Domain.Rewards;
using CSharpFunctionalExtensions;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace ClanWarsService.Core.Features;

public sealed record CreateRewardTableCommand(CreateRewardTableRequest Request) : ICommand;

public sealed record ListRewardTablesQuery : IQuery;

public sealed record GetSeasonRewardsQuery(Guid SeasonId, string? Status, int Limit) : IQuery;

public sealed record RetryFailedRewardsCommand(Guid SeasonId) : ICommand;

public sealed record GetMyRewardsQuery(Guid SeasonId, Guid AccountId) : IQuery;

public static class RewardMapper
{
    public static RewardTableDto ToDto(this RewardTable table) =>
        new(table.Id, table.Name, table.CreatedAt, [.. table.Tiers.Select(tier => new RewardTierDto(
            tier.FromRank, tier.ToRank, tier.ClanPoints, tier.CurrencyCode, tier.CurrencyAmount,
            [.. tier.Items.Select(ToDto)], tier.MinBattles))]);

    public static RewardGrantDto ToDto(this SeasonRewardGrant grant) =>
        new(
            grant.Id,
            grant.SeasonId,
            grant.ClanId,
            grant.AccountId,
            grant.Rank,
            grant.ClanPoints,
            grant.CurrencyCode,
            grant.CurrencyAmount,
            [.. grant.Items.Select(ToDto)],
            grant.Status.ToString().ToUpperInvariant(),
            grant.Attempts,
            grant.LastError,
            grant.CreatedAt,
            grant.CompletedAt);

    private static RewardItemDto ToDto(RewardItem item) => new(item.ItemId, item.Code, item.Kind, item.Slot, item.Quantity);
}

public sealed class CreateRewardTableHandler(
    IRewardRepository rewardRepository,
    ITransactionManager transactionManager,
    TimeProvider timeProvider) : ICommandHandler<RewardTableDto, CreateRewardTableCommand>
{
    public async Task<Result<RewardTableDto, Error>> Handle(CreateRewardTableCommand command, CancellationToken cancellationToken = default)
    {
        var tableResult = RewardTable.Create(
            command.Request.Name,
            [.. (command.Request.Tiers ?? []).Select(tier => new RewardTierData(
                tier.FromRank,
                tier.ToRank,
                tier.ClanPoints,
                tier.CurrencyCode,
                tier.CurrencyAmount,
                [.. (tier.Items ?? []).Select(item => new RewardItem(item.ItemId, item.Code, item.Kind, item.Slot, item.Quantity))],
                tier.MinBattles))],
            timeProvider.GetUtcNow().UtcDateTime);

        if (tableResult.IsFailure)
        {
            return tableResult.Error;
        }

        await rewardRepository.AddTableAsync(tableResult.Value, cancellationToken);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? saveResult.Error : tableResult.Value.ToDto();
    }
}

public sealed class ListRewardTablesHandler(IRewardRepository rewardRepository) : IQueryHandler<IReadOnlyList<RewardTableDto>, ListRewardTablesQuery>
{
    public async Task<Result<IReadOnlyList<RewardTableDto>, Error>> Handle(ListRewardTablesQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RewardTable> tables = await rewardRepository.ListTablesAsync(cancellationToken);
        return tables.Select(table => table.ToDto()).ToList();
    }
}

public sealed class GetSeasonRewardsHandler(IRewardRepository rewardRepository) : IQueryHandler<SeasonRewardsDto, GetSeasonRewardsQuery>
{
    public async Task<Result<SeasonRewardsDto, Error>> Handle(GetSeasonRewardsQuery query, CancellationToken cancellationToken = default)
    {
        RewardGrantStatus? status = Enum.TryParse(query.Status, ignoreCase: true, out RewardGrantStatus parsed) ? parsed : null;

        RewardSummary summary = await rewardRepository.SummarizeAsync(query.SeasonId, cancellationToken);
        IReadOnlyList<SeasonRewardGrant> grants = await rewardRepository.ListAsync(query.SeasonId, status, null, Math.Clamp(query.Limit, 1, 1000), cancellationToken);

        return new SeasonRewardsDto(summary.Pending, summary.Granted, summary.Failed, [.. grants.Select(grant => grant.ToDto())]);
    }
}

/// <summary>
/// After the cause is fixed (currency disabled, item missing), failed grants go back to the queue;
/// steps already done are not repeated.
/// </summary>
public sealed class RetryFailedRewardsHandler(
    IRewardRepository rewardRepository,
    ITransactionManager transactionManager,
    TimeProvider timeProvider) : ICommandHandler<int, RetryFailedRewardsCommand>
{
    public async Task<Result<int, Error>> Handle(RetryFailedRewardsCommand command, CancellationToken cancellationToken = default)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        IReadOnlyList<SeasonRewardGrant> failed = await rewardRepository.ListFailedAsync(command.SeasonId, cancellationToken);

        foreach (SeasonRewardGrant grant in failed)
        {
            grant.Retry(now);
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? saveResult.Error : failed.Count;
    }
}

public sealed class GetMyRewardsHandler(IRewardRepository rewardRepository) : IQueryHandler<IReadOnlyList<RewardGrantDto>, GetMyRewardsQuery>
{
    public async Task<Result<IReadOnlyList<RewardGrantDto>, Error>> Handle(GetMyRewardsQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SeasonRewardGrant> grants = await rewardRepository.ListAsync(query.SeasonId, null, query.AccountId, 100, cancellationToken);
        return grants.Select(grant => grant.ToDto()).ToList();
    }
}

internal static class RewardTableCheck
{
    public static async Task<UnitResult<Error>> EnsureExistsAsync(IRewardRepository rewardRepository, Guid? tableId, CancellationToken cancellationToken) =>
        tableId is null || await rewardRepository.GetTableAsync(tableId.Value, cancellationToken) is not null
            ? UnitResult.Success<Error>()
            : ClanWarsErrors.RewardTableNotFound;
}
