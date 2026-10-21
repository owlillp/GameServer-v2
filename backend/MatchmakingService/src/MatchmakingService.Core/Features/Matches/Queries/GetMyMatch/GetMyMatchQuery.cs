using Shared.Core.Abstractions;

namespace MatchmakingService.Core.Features.Matches.Queries.GetMyMatch;

public sealed record GetMyMatchQuery(Guid AccountId) : IQuery;
