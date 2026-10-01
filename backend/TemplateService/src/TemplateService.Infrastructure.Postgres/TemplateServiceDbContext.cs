using TemplateService.Core.Abstractions;
using TemplateService.Domain;
using Microsoft.EntityFrameworkCore;

namespace TemplateService.Infrastructure.Postgres;

public class TemplateServiceDbContext(DbContextOptions<TemplateServiceDbContext> options)
    : DbContext(options), IReadDbContext
{
    public DbSet<Item> Items => Set<Item>();

    public IQueryable<Item> ItemsRead => Set<Item>()
        .AsQueryable()
        .AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Constants.SCHEMA);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TemplateServiceDbContext).Assembly);
    }
}
