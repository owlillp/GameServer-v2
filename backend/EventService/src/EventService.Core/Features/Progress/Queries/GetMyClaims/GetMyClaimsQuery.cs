using EventService.Contracts.Dtos;
using Shared.Core.Abstractions;

namespace EventService.Core.Features.Progress.Queries.GetMyClaims;

public sealed record GetMyClaimsQuery(Guid AccountId, Guid? EventId = null)
    : IQuery;
