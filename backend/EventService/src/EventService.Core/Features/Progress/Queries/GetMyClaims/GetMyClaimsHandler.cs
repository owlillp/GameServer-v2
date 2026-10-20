using CSharpFunctionalExtensions;
using EventService.Contracts.Dtos;
using EventService.Core.Abstractions;
using EventService.Core.Features.Events.Models;
using EventService.Domain.Progress;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace EventService.Core.Features.Progress.Queries.GetMyClaims;

public sealed class GetMyClaimsHandler(IEventParticipationRepository participationRepository)
    : IQueryHandler<IReadOnlyList<EventRewardClaimDto>, GetMyClaimsQuery>
{
    public async Task<Result<IReadOnlyList<EventRewardClaimDto>, Error>> Handle(
        GetMyClaimsQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<EventRewardClaim> claims = await participationRepository.ListClaimsAsync(
            query.AccountId,
            query.EventId,
            cancellationToken);

        return claims
            .OrderByDescending(claim => claim.CreatedAt)
            .Select(EventMapper.ToClaimDto)
            .ToList();
    }
}
