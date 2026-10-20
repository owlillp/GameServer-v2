using CSharpFunctionalExtensions;
using EventService.Domain.Progress;
using Shared.SharedKernel.Errors;

namespace EventService.Core.Abstractions;

public interface IEventParticipationRepository
{
    Task<EventProgress?> FindProgressAsync(
        Guid eventId,
        Guid accountId,
        CancellationToken cancellationToken);

    Task<Result<EventProgress, Error>> GetProgressAsync(
        Guid eventId,
        Guid accountId,
        CancellationToken cancellationToken);

    Task<EventProgress> GetOrCreateProgressAsync(
        Guid eventId,
        Guid accountId,
        CancellationToken cancellationToken);

    Task<EventQuestProgress?> FindQuestProgressAsync(
        Guid questId,
        Guid accountId,
        DateTime periodStart,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EventQuestProgress>> ListQuestProgressAsync(
        Guid eventId,
        Guid accountId,
        CancellationToken cancellationToken);

    Task AddQuestProgressAsync(EventQuestProgress progress, CancellationToken cancellationToken);

    Task<bool> MatchProcessedAsync(
        Guid eventId,
        Guid accountId,
        Guid matchId,
        CancellationToken cancellationToken);

    Task AddMatchReceiptAsync(EventMatchReceipt receipt, CancellationToken cancellationToken);

    Task<Result<EventRewardClaim, Error>> GetClaimAsync(Guid claimId, CancellationToken cancellationToken);

    Task<EventRewardClaim?> FindClaimByIdempotencyKeyAsync(
        Guid accountId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<bool> HasClaimAsync(Guid accountId, Guid rewardId, CancellationToken cancellationToken);

    Task<IReadOnlyList<EventRewardClaim>> ListClaimsAsync(
        Guid accountId,
        Guid? eventId,
        CancellationToken cancellationToken);

    Task AddClaimAsync(EventRewardClaim claim, CancellationToken cancellationToken);
}
