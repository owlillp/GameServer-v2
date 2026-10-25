using ClanWarsService.Domain.Battles;
using ClanWarsService.Domain.Rewards;
using ClanWarsService.Domain.Roster;
using ClanWarsService.Domain.Scores;
using ClanWarsService.Domain.Seasons;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace ClanWarsService.Infrastructure.Postgres;

public class ClanWarsServiceDbContext(DbContextOptions<ClanWarsServiceDbContext> options) : DbContext(options)
{
    public DbSet<Season> Seasons => Set<Season>();

    public DbSet<ClanProjection> Clans => Set<ClanProjection>();

    public DbSet<RosterEntry> Roster => Set<RosterEntry>();

    public DbSet<SeasonScore> Scores => Set<SeasonScore>();

    public DbSet<ScoreReceipt> Receipts => Set<ScoreReceipt>();

    public DbSet<MemberSeasonStats> MemberStats => Set<MemberSeasonStats>();

    public DbSet<SeasonResult> Results => Set<SeasonResult>();

    public DbSet<BattleWindow> BattleWindows => Set<BattleWindow>();

    public DbSet<BattleQueueEntry> BattleQueue => Set<BattleQueueEntry>();

    public DbSet<Battle> Battles => Set<Battle>();

    public DbSet<RewardTable> RewardTables => Set<RewardTable>();

    public DbSet<SeasonRewardGrant> RewardGrants => Set<SeasonRewardGrant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Constants.SCHEMA);
        modelBuilder.MapWolverineEnvelopeStorage("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClanWarsServiceDbContext).Assembly);
    }
}
