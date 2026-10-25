using ClanService.Core.Abstractions;
using ClanService.Domain;
using ClanService.Domain.Clans;
using ClanService.Domain.Points;
using CSharpFunctionalExtensions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace ClanService.Core.Services;

/// <summary>
/// The only way to change clan points: a ledger entry (unique operation id) and an atomic
/// <c>clan_points + amount</c> in one transaction. A repeated operation returns
/// <see cref="ClanErrors.PointsAlreadyApplied"/> and changes nothing.
/// </summary>
public sealed class ClanPointsService(
    IClanRepository clanRepository,
    IClanPointsRepository pointsRepository,
    ITransactionManager transactionManager,
    TimeProvider timeProvider)
{
    public async Task<Result<ClanPointsEntry, Error>> ApplyAsync(
        Guid clanId,
        Guid operationId,
        long amount,
        string reason,
        Guid? actorId,
        string? comment,
        CancellationToken cancellationToken)
    {
        Clan? clan = await clanRepository.GetAsync(clanId, cancellationToken);
        if (clan is null)
        {
            return ClanErrors.ClanNotFound;
        }

        var entryResult = ClanPointsEntry.Create(
            clanId,
            operationId,
            amount,
            reason,
            actorId,
            comment,
            timeProvider.GetUtcNow().UtcDateTime);

        if (entryResult.IsFailure)
        {
            return entryResult.Error;
        }

        var beginResult = await transactionManager.BeginTransactionAsync(cancellationToken);
        if (beginResult.IsFailure)
        {
            return beginResult.Error;
        }

        await pointsRepository.AddAsync(entryResult.Value, cancellationToken);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error.IsUniqueViolation() ? ClanErrors.PointsAlreadyApplied : saveResult.Error;
        }

        await clanRepository.AddPointsAsync(clanId, amount, cancellationToken);

        var commitResult = await transactionManager.CommitTransactionAsync(cancellationToken);
        return commitResult.IsFailure ? commitResult.Error : entryResult.Value;
    }
}
