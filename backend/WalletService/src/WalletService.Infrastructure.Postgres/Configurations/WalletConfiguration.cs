using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WalletService.Domain.Wallets;

namespace WalletService.Infrastructure.Postgres.Configurations;

internal sealed class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("wallets");

        builder.Property(wallet => wallet.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(wallet => wallet.CreatedAt).HasColumnName("created_at");
        builder.Property(wallet => wallet.UpdatedAt).HasColumnName("updated_at");

        builder.HasKey(wallet => wallet.Id);

        builder.HasMany(wallet => wallet.Balances)
            .WithOne()
            .HasForeignKey(balance => balance.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(wallet => wallet.Balances)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
