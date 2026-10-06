using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WalletService.Domain.Wallets;

namespace WalletService.Infrastructure.Postgres.Configurations;

internal sealed class BalanceConfiguration : IEntityTypeConfiguration<Balance>
{
    public void Configure(EntityTypeBuilder<Balance> builder)
    {
        builder.ToTable("balances");

        builder.Property(balance => balance.WalletId).HasColumnName("wallet_id");

        builder.Property(balance => balance.Currency)
            .HasColumnName("currency")
            .HasConversion(currency => currency.Code, code => Currency.Create(code).Value)
            .HasMaxLength(Currency.MAX_LENGTH);

        builder.Property(balance => balance.CreatedAt).HasColumnName("created_at");
        builder.Property(balance => balance.UpdatedAt).HasColumnName("updated_at");
        builder.Property(balance => balance.Available).HasColumnName("available");

        builder.HasKey(balance => new { balance.WalletId, balance.Currency });
    }
}
