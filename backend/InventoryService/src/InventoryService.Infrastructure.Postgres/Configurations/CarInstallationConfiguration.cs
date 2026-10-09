using InventoryService.Domain.Inventories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Infrastructure.Postgres.Configurations;

internal sealed class CarInstallationConfiguration : IEntityTypeConfiguration<CarInstallation>
{
    public void Configure(EntityTypeBuilder<CarInstallation> builder)
    {
        builder.ToTable("car_installations");

        builder.Property(installation => installation.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(installation => installation.CarId).HasColumnName("car_id");

        builder.Property(installation => installation.Slot)
            .HasColumnName("slot")
            .HasMaxLength(InventoryConstants.SLOT_MAX_LENGTH)
            .IsRequired();

        builder.Property(installation => installation.PartItemId).HasColumnName("part_item_id");

        builder.HasKey(installation => installation.Id);

        builder.HasIndex(installation => new { installation.CarId, installation.Slot })
            .IsUnique()
            .HasDatabaseName("uq_car_installations_car_slot");
    }
}
