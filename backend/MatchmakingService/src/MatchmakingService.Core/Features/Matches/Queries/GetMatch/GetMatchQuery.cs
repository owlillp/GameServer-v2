using Shared.Core.Abstractions;

namespace MatchmakingService.Core.Features.Matches.Queries.GetMatch;

public sealed record GetMatchQuery(Guid MatchId, Guid AccountId) : IQuery;
