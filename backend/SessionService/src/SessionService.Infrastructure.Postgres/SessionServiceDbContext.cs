using Microsoft.EntityFrameworkCore;
using SessionService.Core.Abstractions;
using SessionService.Domain.Reports;
using SessionService.Domain.Servers;
using SessionService.Domain.Sessions;
using Wolverine.EntityFrameworkCore;

namespace SessionService.Infrastructure.Postgres;

public class SessionServiceDbContext(DbContextOptions<SessionServiceDbContext> options)
    : DbContext(options), IReadDbContext
{
    public DbSet<GameServer> GameServers => Set<GameServer>();

    public DbSet<Session> Sessions => Set<Session>();

    public DbSet<SessionPlayer> SessionPlayers => Set<SessionPlayer>();

    public DbSet<RawReport> RawReports => Set<RawReport>();

    public IQueryable<GameServer> ServersRead => GameServers.AsQueryable().AsNoTracking();

    public IQueryable<Session> SessionsRead => Sessions.AsQueryable().AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Constants.SCHEMA);
        modelBuilder.MapWolverineEnvelopeStorage("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SessionServiceDbContext).Assembly);
    }
}
