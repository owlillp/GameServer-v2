using AuthService.Core.Abstractions;
using AuthService.Domain.Accounts;
using AuthService.Domain.Authorization;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Postgres;

public class AuthServiceDbContext(DbContextOptions<AuthServiceDbContext> options)
    : DbContext(options), IReadDbContext
{
    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Role> Roles => Set<Role>();

    public IQueryable<Account> AccountsRead => Set<Account>()
        .AsQueryable()
        .AsNoTracking();

    public IQueryable<Role> RolesRead => Set<Role>()
        .AsQueryable()
        .AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Constants.SCHEMA);
        modelBuilder.UseOpenIddict();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthServiceDbContext).Assembly);
    }
}
