using CatalogService.Domain.Versions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogService.Infrastructure.Postgres.Configurations;

internal sealed class CatalogVersionConfiguration : IEntityTypeConfiguration<CatalogVersion>
{
    public void Configure(EntityTypeBuilder<CatalogVersion> builder)
    {
        builder.ToTable("catalog_versions");

        builder.Property(version => version.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(version => version.Number)
            .HasColumnName("number")
            .IsRequired();

        builder.Property(version => version.Note)
            .HasColumnName("note")
            .HasMaxLength(512);

        builder.Property(version => version.PublishedBy).HasColumnName("published_by");
        builder.Property(version => version.PublishedAt).HasColumnName("published_at");

        builder.HasKey(version => version.Id);

        builder.HasIndex(version => version.Number)
            .IsUnique()
            .HasDatabaseName("uq_catalog_versions_number");

        builder.HasMany(version => version.Items)
            .WithOne()
            .HasForeignKey(item => item.VersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(version => version.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
