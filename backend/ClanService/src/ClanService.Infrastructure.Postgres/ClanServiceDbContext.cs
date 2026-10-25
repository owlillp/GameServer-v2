using ClanService.Domain.Clans;
using ClanService.Domain.Members;
using ClanService.Domain.Points;
using ClanService.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace ClanService.Infrastructure.Postgres;

public class ClanServiceDbContext(DbContextOptions<ClanServiceDbContext> options) : DbContext(options)
{
    public DbSet<Clan> Clans => Set<Clan>();

    public DbSet<ClanMember> Members => Set<ClanMember>();

    public DbSet<ClanMemberHistory> MemberHistory => Set<ClanMemberHistory>();

    public DbSet<ClanJoinRequest> JoinRequests => Set<ClanJoinRequest>();

    public DbSet<ClanPointsEntry> PointsLedger => Set<ClanPointsEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Constants.SCHEMA);
        modelBuilder.MapWolverineEnvelopeStorage("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClanServiceDbContext).Assembly);
    }
}
