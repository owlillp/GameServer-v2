using CatalogService.Domain.Items;
using CatalogService.Domain.Versions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogService.Infrastructure.Postgres.Configurations;

internal sealed class CatalogVersionItemConfiguration : IEntityTypeConfiguration<CatalogVersionItem>
{
    public void Configure(EntityTypeBuilder<CatalogVersionItem> builder)
    {
        builder.ToTable("catalog_version_items");

        builder.Property(item => item.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(item => item.VersionId).HasColumnName("version_id");
        builder.Property(item => item.ItemId).HasColumnName("item_id");

        builder.Property(item => item.Code)
            .HasColumnName("code")
            .HasMaxLength(CatalogConstants.CODE_MAX_LENGTH)
            .IsRequired();

        builder.Property(item => item.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(item => item.Snapshot)
            .HasColumnName("snapshot")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasKey(item => item.Id);

        builder.HasIndex(item => new { item.VersionId, item.ItemId })
            .IsUnique()
            .HasDatabaseName("uq_catalog_version_items_version_item");
    }
}
