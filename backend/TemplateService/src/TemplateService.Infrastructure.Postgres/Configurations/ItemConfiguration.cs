using TemplateService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TemplateService.Infrastructure.Postgres.Configurations;

internal sealed class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("items");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");

        builder.Property(item => item.Name)
            .HasColumnName("name")
            .HasMaxLength(ItemConstants.NAME_MAX_LENGTH)
            .IsRequired();

        builder.Property(item => item.NormalizedName)
            .HasColumnName("normalized_name")
            .HasMaxLength(ItemConstants.NAME_MAX_LENGTH)
            .IsRequired();

        builder.Property(item => item.Description)
            .HasColumnName("description")
            .HasMaxLength(ItemConstants.DESCRIPTION_MAX_LENGTH);

        builder.Property(item => item.OwnerId).HasColumnName("owner_id");
        builder.Property(item => item.CreatedAt).HasColumnName("created_at");
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(item => item.NormalizedName);
        builder.HasIndex(item => item.OwnerId);
        builder.HasIndex(item => item.CreatedAt);
    }
}
