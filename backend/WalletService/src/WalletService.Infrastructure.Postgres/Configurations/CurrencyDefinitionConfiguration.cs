using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WalletService.Domain.Wallets;

namespace WalletService.Infrastructure.Postgres.Configurations;

internal sealed class CurrencyDefinitionConfiguration : IEntityTypeConfiguration<CurrencyDefinition>
{
    public void Configure(EntityTypeBuilder<CurrencyDefinition> builder)
    {
        builder.ToTable("currencies");

        builder.Property(currency => currency.Code)
            .HasColumnName("code")
            .HasMaxLength(Currency.MAX_LENGTH)
            .ValueGeneratedNever();

        builder.Property(currency => currency.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(WalletConstants.CURRENCY_DISPLAY_NAME_MAX_LENGTH);

        builder.Property(currency => currency.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(currency => currency.IsActive).HasColumnName("is_active");
        builder.Property(currency => currency.CreatedAt).HasColumnName("created_at");

        builder.HasKey(currency => currency.Code);
    }
}
