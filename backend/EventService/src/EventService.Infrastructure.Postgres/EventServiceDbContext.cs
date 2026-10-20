using EventService.Core.Abstractions;
using EventService.Domain.Events;
using EventService.Domain.Progress;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace EventService.Infrastructure.Postgres;

public class EventServiceDbContext(DbContextOptions<EventServiceDbContext> options)
    : DbContext(options), IReadDbContext
{
    public DbSet<GameEvent> Events => Set<GameEvent>();

    public DbSet<EventQuest> EventQuests => Set<EventQuest>();

    public DbSet<EventReward> EventRewards => Set<EventReward>();

    public DbSet<EventProgress> Progress => Set<EventProgress>();

    public DbSet<EventQuestProgress> QuestProgress => Set<EventQuestProgress>();

    public DbSet<EventRewardClaim> RewardClaims => Set<EventRewardClaim>();

    public DbSet<EventMatchReceipt> MatchReceipts => Set<EventMatchReceipt>();

    public IQueryable<GameEvent> EventsRead => Events.AsQueryable().AsNoTracking();

    public IQueryable<EventProgress> ProgressRead => Progress.AsQueryable().AsNoTracking();

    public IQueryable<EventQuestProgress> QuestProgressRead => QuestProgress.AsQueryable().AsNoTracking();

    public IQueryable<EventRewardClaim> ClaimsRead => RewardClaims.AsQueryable().AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Constants.SCHEMA);
        modelBuilder.MapWolverineEnvelopeStorage("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventServiceDbContext).Assembly);
    }
}
