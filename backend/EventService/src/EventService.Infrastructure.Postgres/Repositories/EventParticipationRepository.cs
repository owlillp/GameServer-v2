using CSharpFunctionalExtensions;
using EventService.Core.Abstractions;
using EventService.Domain;
using EventService.Domain.Progress;
using Microsoft.EntityFrameworkCore;
using Shared.SharedKernel.Errors;

namespace EventService.Infrastructure.Postgres.Repositories;

public sealed class EventParticipationRepository(EventServiceDbContext dbContext) : IEventParticipationRepository
{
    public Task<EventProgress?> FindProgressAsync(
        Guid eventId,
        Guid accountId,
        CancellationToken cancellationToken) =>
        dbContext.Progress.FirstOrDefaultAsync(
            progress => progress.EventId == eventId && progress.AccountId == accountId,
            cancellationToken);

    public async Task<Result<EventProgress, Error>> GetProgressAsync(
        Guid eventId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        EventProgress? progress = await FindProgressAsync(eventId, accountId, cancellationToken);

        return progress is not null ? progress : EventErrors.ProgressNotFound;
    }

    public async Task<EventProgress> GetOrCreateProgressAsync(
        Guid eventId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        EventProgress? progress = await FindProgressAsync(eventId, accountId, cancellationToken);
        if (progress is not null)
        {
            return progress;
        }

        EventProgress created = EventProgress.Create(eventId, accountId).Value;
        dbContext.Progress.Add(created);

        return created;
    }

    public Task<EventQuestProgress?> FindQuestProgressAsync(
        Guid questId,
        Guid accountId,
        DateTime periodStart,
        CancellationToken cancellationToken) =>
        dbContext.QuestProgress.FirstOrDefaultAsync(
            progress => progress.QuestId == questId
                && progress.AccountId == accountId
                && progress.PeriodStart == periodStart,
            cancellationToken);

    public async Task<IReadOnlyList<EventQuestProgress>> ListQuestProgressAsync(
        Guid eventId,
        Guid accountId,
        CancellationToken cancellationToken) =>
        await dbContext.QuestProgress
            .AsNoTracking()
            .Where(progress => progress.EventId == eventId && progress.AccountId == accountId)
            .ToListAsync(cancellationToken);

    public Task AddQuestProgressAsync(EventQuestProgress progress, CancellationToken cancellationToken)
    {
        dbContext.QuestProgress.Add(progress);
        return Task.CompletedTask;
    }

    public Task<bool> MatchProcessedAsync(
        Guid eventId,
        Guid accountId,
        Guid matchId,
        CancellationToken cancellationToken) =>
        dbContext.MatchReceipts.AnyAsync(
            receipt => receipt.EventId == eventId
                && receipt.AccountId == accountId
                && receipt.MatchId == matchId,
            cancellationToken);

    public Task AddMatchReceiptAsync(EventMatchReceipt receipt, CancellationToken cancellationToken)
    {
        dbContext.MatchReceipts.Add(receipt);
        return Task.CompletedTask;
    }

    public async Task<Result<EventRewardClaim, Error>> GetClaimAsync(
        Guid claimId,
        CancellationToken cancellationToken)
    {
        EventRewardClaim? claim = await dbContext.RewardClaims.FirstOrDefaultAsync(
            entity => entity.Id == claimId,
            cancellationToken);

        return claim is not null ? claim : EventErrors.ClaimNotFound;
    }

    public Task<EventRewardClaim?> FindClaimByIdempotencyKeyAsync(
        Guid accountId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        string normalizedKey = idempotencyKey.Trim();

        return dbContext.RewardClaims.FirstOrDefaultAsync(
            claim => claim.AccountId == accountId && claim.IdempotencyKey == normalizedKey,
            cancellationToken);
    }

    public Task<bool> HasClaimAsync(Guid accountId, Guid rewardId, CancellationToken cancellationToken) =>
        dbContext.RewardClaims.AnyAsync(
            claim => claim.AccountId == accountId
                && claim.RewardId == rewardId
                && claim.Status != EventRewardClaimStatus.FAILED,
            cancellationToken);

    public async Task<IReadOnlyList<EventRewardClaim>> ListClaimsAsync(
        Guid accountId,
        Guid? eventId,
        CancellationToken cancellationToken) =>
        await dbContext.RewardClaims
            .AsNoTracking()
            .Where(claim => claim.AccountId == accountId && (eventId == null || claim.EventId == eventId))
            .ToListAsync(cancellationToken);

    public Task AddClaimAsync(EventRewardClaim claim, CancellationToken cancellationToken)
    {
        dbContext.RewardClaims.Add(claim);
        return Task.CompletedTask;
    }
}
