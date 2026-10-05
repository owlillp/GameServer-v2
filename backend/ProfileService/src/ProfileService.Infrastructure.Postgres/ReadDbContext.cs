using Microsoft.EntityFrameworkCore;
using ProfileService.Core.Abstractions;
using ProfileService.Domain.Players;

namespace ProfileService.Infrastructure.Postgres;

public class ReadDbContext(DbContextOptions<ReadDbContext> options)
    : DbContext(options), IReadDbContext
{
    public IQueryable<PlayerProfile> Profiles => Set<PlayerProfile>()
        .AsQueryable()
        .AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Constants.SCHEMA);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReadDbContext).Assembly);
    }
}
