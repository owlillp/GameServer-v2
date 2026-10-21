using Shared.Core.Abstractions;

namespace MatchmakingService.Core.Features.Matches.Queries.GetAdminMatch;

public sealed record GetAdminMatchQuery(Guid MatchId) : IQuery;
