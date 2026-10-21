using Shared.Core.Abstractions;

namespace MatchmakingService.Core.Features.Leaderboard.Queries.GetMyLeaderboard;

public sealed record GetMyLeaderboardQuery(Guid AccountId) : IQuery;
