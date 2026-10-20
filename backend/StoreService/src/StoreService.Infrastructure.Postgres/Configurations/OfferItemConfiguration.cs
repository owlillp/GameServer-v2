using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreService.Domain.Offers;

namespace StoreService.Infrastructure.Postgres.Configurations;

internal sealed class OfferItemConfiguration : IEntityTypeConfiguration<OfferItem>
{
    public void Configure(EntityTypeBuilder<OfferItem> builder)
    {
        builder.ToTable("offer_items");

        builder.Property(item => item.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(item => item.OfferId).HasColumnName("offer_id");
        builder.Property(item => item.CatalogItemId).HasColumnName("catalog_item_id");

        builder.Property(item => item.Code)
            .HasColumnName("code")
            .HasMaxLength(Domain.StoreConstants.CODE_MAX_LENGTH);

        builder.Property(item => item.Kind)
            .HasColumnName("kind")
            .HasMaxLength(32);

        builder.Property(item => item.Slot)
            .HasColumnName("slot")
            .HasMaxLength(32);

        builder.Property(item => item.Quantity).HasColumnName("quantity");

        builder.HasKey(item => item.Id);
    }
}
