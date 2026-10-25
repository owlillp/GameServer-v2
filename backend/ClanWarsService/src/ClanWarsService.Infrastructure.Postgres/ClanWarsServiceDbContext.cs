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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Constants.SCHEMA);
        modelBuilder.MapWolverineEnvelopeStorage("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClanWarsServiceDbContext).Assembly);
    }
}
