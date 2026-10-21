using Shared.Core.Abstractions;

namespace MatchmakingService.Core.Features.Leaderboard.Queries.GetLeaderboard;

public sealed record GetLeaderboardQuery(int Limit) : IQuery;
