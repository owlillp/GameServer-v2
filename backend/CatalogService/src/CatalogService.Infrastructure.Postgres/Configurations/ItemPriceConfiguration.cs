using CatalogService.Domain.Items;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogService.Infrastructure.Postgres.Configurations;

internal sealed class ItemPriceConfiguration : IEntityTypeConfiguration<ItemPrice>
{
    public void Configure(EntityTypeBuilder<ItemPrice> builder)
    {
        builder.ToTable("item_prices");

        builder.Property(price => price.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(price => price.ItemId).HasColumnName("item_id");

        builder.Property(price => price.CurrencyCode)
            .HasColumnName("currency_code")
            .HasMaxLength(CatalogConstants.CURRENCY_MAX_LENGTH)
            .IsRequired();

        builder.Property(price => price.Amount).HasColumnName("amount");
        builder.Property(price => price.IsActive).HasColumnName("is_active");
        builder.Property(price => price.ValidFrom).HasColumnName("valid_from");

        builder.HasKey(price => price.Id);

        builder.HasIndex(price => new { price.ItemId, price.CurrencyCode })
            .IsUnique()
            .HasDatabaseName("uq_item_prices_item_currency");
    }
}
