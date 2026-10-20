using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreService.Domain;
using StoreService.Domain.Offers;

namespace StoreService.Infrastructure.Postgres.Configurations;

internal sealed class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.ToTable("offers");

        builder.Property(offer => offer.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(offer => offer.Code)
            .HasColumnName("code")
            .HasMaxLength(StoreConstants.CODE_MAX_LENGTH);

        builder.Property(offer => offer.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(StoreConstants.DISPLAY_NAME_MAX_LENGTH);

        builder.Property(offer => offer.Description)
            .HasColumnName("description")
            .HasMaxLength(StoreConstants.DESCRIPTION_MAX_LENGTH);

        builder.Property(offer => offer.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(offer => offer.PriceCurrency)
            .HasColumnName("price_currency")
            .HasMaxLength(StoreConstants.CURRENCY_MAX_LENGTH);

        builder.Property(offer => offer.PriceAmount).HasColumnName("price_amount");
        builder.Property(offer => offer.StartsAt).HasColumnName("starts_at");
        builder.Property(offer => offer.EndsAt).HasColumnName("ends_at");
        builder.Property(offer => offer.SortOrder).HasColumnName("sort_order");
        builder.Property(offer => offer.Version).HasColumnName("version").IsConcurrencyToken();
        builder.Property(offer => offer.CreatedAt).HasColumnName("created_at");
        builder.Property(offer => offer.UpdatedAt).HasColumnName("updated_at");

        builder.HasKey(offer => offer.Id);

        builder.HasIndex(offer => offer.Code)
            .IsUnique()
            .HasDatabaseName("uq_offers_code");

        builder.HasMany(offer => offer.Items)
            .WithOne()
            .HasForeignKey(item => item.OfferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(offer => offer.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
