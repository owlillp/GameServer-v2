using Shared.Core.Abstractions;

namespace MatchmakingService.Core.Features.Matches.Queries.ListMatches;

public sealed record ListMatchesQuery(string? Status, int Limit) : IQuery;
