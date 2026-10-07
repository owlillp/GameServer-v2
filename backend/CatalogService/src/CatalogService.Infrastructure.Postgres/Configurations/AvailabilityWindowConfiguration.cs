using CatalogService.Domain.Items;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogService.Infrastructure.Postgres.Configurations;

internal sealed class AvailabilityWindowConfiguration : IEntityTypeConfiguration<AvailabilityWindow>
{
    public void Configure(EntityTypeBuilder<AvailabilityWindow> builder)
    {
        builder.ToTable("availability_windows");

        builder.Property(window => window.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(window => window.ItemId).HasColumnName("item_id");
        builder.Property(window => window.StartsAt).HasColumnName("starts_at");
        builder.Property(window => window.EndsAt).HasColumnName("ends_at");

        builder.Property(window => window.Channel)
            .HasColumnName("channel")
            .HasMaxLength(CatalogConstants.SLOT_MAX_LENGTH);

        builder.HasKey(window => window.Id);

        builder.HasIndex(window => window.ItemId)
            .HasDatabaseName("ix_availability_windows_item");
    }
}
