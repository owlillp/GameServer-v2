using Microsoft.EntityFrameworkCore;
using WalletService.Core.Abstractions;
using WalletService.Domain.Wallets;
using WalletService.Domain.Wallets.Ledgers;
using Wolverine.EntityFrameworkCore;

namespace WalletService.Infrastructure.Postgres;

public class WalletServiceDbContext(DbContextOptions<WalletServiceDbContext> options)
    : DbContext(options), IReadDbContext
{
    public DbSet<Wallet> Wallets => Set<Wallet>();

    public DbSet<Balance> Balances => Set<Balance>();

    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    public DbSet<CurrencyDefinition> Currencies => Set<CurrencyDefinition>();

    public IQueryable<Wallet> WalletsRead => Wallets.AsQueryable().AsNoTracking();

    public IQueryable<LedgerEntry> LedgerRead => LedgerEntries.AsQueryable().AsNoTracking();

    public IQueryable<CurrencyDefinition> CurrenciesRead => Currencies.AsQueryable().AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Constants.SCHEMA);
        modelBuilder.MapWolverineEnvelopeStorage("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WalletServiceDbContext).Assembly);
    }
}
