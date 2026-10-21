using MatchmakingService.Core.Abstractions;
using MatchmakingService.Domain.Leaderboard;
using MatchmakingService.Domain.Matches;
using MatchmakingService.Domain.Parties;
using MatchmakingService.Domain.Queues;
using MatchmakingService.Domain.Rewards;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace MatchmakingService.Infrastructure.Postgres;

public class MatchmakingServiceDbContext(DbContextOptions<MatchmakingServiceDbContext> options)
    : DbContext(options), IReadDbContext
{
    public DbSet<Party> Parties => Set<Party>();

    public DbSet<PartyMember> PartyMembers => Set<PartyMember>();

    public DbSet<QueueTicket> QueueTickets => Set<QueueTicket>();

    public DbSet<Match> Matches => Set<Match>();

    public DbSet<MatchParty> MatchParties => Set<MatchParty>();

    public DbSet<MatchParticipant> MatchParticipants => Set<MatchParticipant>();

    public DbSet<MatchResult> MatchResults => Set<MatchResult>();

    public DbSet<MatchRewardDefinition> MatchRewardDefinitions => Set<MatchRewardDefinition>();

    public DbSet<MatchRewardDefinitionItem> MatchRewardDefinitionItems => Set<MatchRewardDefinitionItem>();

    public DbSet<MatchRewardGrant> MatchRewardGrants => Set<MatchRewardGrant>();

    public DbSet<MatchRewardGrantItem> MatchRewardGrantItems => Set<MatchRewardGrantItem>();

    public DbSet<LeaderboardEntry> LeaderboardEntries => Set<LeaderboardEntry>();

    public IQueryable<Party> PartiesRead => Parties.AsQueryable().AsNoTracking();

    public IQueryable<Match> MatchesRead => Matches.AsQueryable().AsNoTracking();

    public IQueryable<QueueTicket> QueueTicketsRead => QueueTickets.AsQueryable().AsNoTracking();

    public IQueryable<MatchRewardGrant> RewardGrantsRead => MatchRewardGrants.AsQueryable().AsNoTracking();

    public IQueryable<LeaderboardEntry> LeaderboardRead => LeaderboardEntries.AsQueryable().AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Constants.SCHEMA);
        modelBuilder.MapWolverineEnvelopeStorage("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MatchmakingServiceDbContext).Assembly);
    }
}
