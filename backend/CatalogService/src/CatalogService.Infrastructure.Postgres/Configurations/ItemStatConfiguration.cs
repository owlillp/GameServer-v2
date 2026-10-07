using CatalogService.Domain.Items;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogService.Infrastructure.Postgres.Configurations;

internal sealed class ItemStatConfiguration : IEntityTypeConfiguration<ItemStat>
{
    public void Configure(EntityTypeBuilder<ItemStat> builder)
    {
        builder.ToTable("item_stats");

        builder.Property(stat => stat.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(stat => stat.ItemId).HasColumnName("item_id");

        builder.Property(stat => stat.Key)
            .HasColumnName("key")
            .HasMaxLength(CatalogConstants.STAT_KEY_MAX_LENGTH)
            .IsRequired();

        builder.Property(stat => stat.Value)
            .HasColumnName("value")
            .HasPrecision(18, 4);

        builder.HasKey(stat => stat.Id);

        builder.HasIndex(stat => new { stat.ItemId, stat.Key })
            .IsUnique()
            .HasDatabaseName("uq_item_stats_item_key");
    }
}
