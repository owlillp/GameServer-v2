using CatalogService.Domain.Items;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogService.Infrastructure.Postgres.Configurations;

internal sealed class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("items");

        builder.Property(item => item.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(item => item.Code)
            .HasColumnName("code")
            .HasMaxLength(CatalogConstants.CODE_MAX_LENGTH)
            .IsRequired();

        builder.Property(item => item.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(item => item.Rarity)
            .HasColumnName("rarity")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(item => item.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(item => item.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(CatalogConstants.DISPLAY_NAME_MAX_LENGTH)
            .IsRequired();

        builder.Property(item => item.Description)
            .HasColumnName("description")
            .HasMaxLength(CatalogConstants.DESCRIPTION_MAX_LENGTH);

        builder.Property(item => item.ModelCode)
            .HasColumnName("model_code")
            .HasMaxLength(CatalogConstants.MODEL_CODE_MAX_LENGTH);

        builder.Property(item => item.Slot)
            .HasColumnName("slot")
            .HasMaxLength(CatalogConstants.SLOT_MAX_LENGTH);

        builder.Property(item => item.Tags)
            .HasColumnName("tags")
            .HasColumnType("text[]");

        builder.Property(item => item.FitTags)
            .HasColumnName("fit_tags")
            .HasColumnType("text[]");

        builder.Property(item => item.LastPublishedVersion).HasColumnName("last_published_version");
        builder.Property(item => item.CreatedAt).HasColumnName("created_at");
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at");

        builder.HasKey(item => item.Id);

        builder.HasIndex(item => item.Code)
            .IsUnique()
            .HasDatabaseName("uq_items_code");

        builder.HasIndex(item => item.Status)
            .HasDatabaseName("ix_items_status");

        builder.HasIndex(item => item.Kind)
            .HasDatabaseName("ix_items_kind");

        builder.HasMany(item => item.Stats)
            .WithOne()
            .HasForeignKey(stat => stat.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(item => item.Prices)
            .WithOne()
            .HasForeignKey(price => price.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(item => item.Availability)
            .WithOne()
            .HasForeignKey(window => window.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(item => item.Stats).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(item => item.Prices).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(item => item.Availability).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
