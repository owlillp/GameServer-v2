using MatchmakingService.Domain.Leaderboard;
using MatchmakingService.Domain.Matches;
using MatchmakingService.Domain.Parties;
using MatchmakingService.Domain.Queues;
using MatchmakingService.Domain.Rewards;

namespace MatchmakingService.Core.Abstractions;

public interface IReadDbContext
{
    IQueryable<Party> PartiesRead { get; }

    IQueryable<Match> MatchesRead { get; }

    IQueryable<QueueTicket> QueueTicketsRead { get; }

    IQueryable<MatchRewardGrant> RewardGrantsRead { get; }

    IQueryable<LeaderboardEntry> LeaderboardRead { get; }
}
