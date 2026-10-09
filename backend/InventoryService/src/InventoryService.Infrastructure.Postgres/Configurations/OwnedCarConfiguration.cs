using InventoryService.Domain.Inventories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Infrastructure.Postgres.Configurations;

internal sealed class OwnedCarConfiguration : IEntityTypeConfiguration<OwnedCar>
{
    public void Configure(EntityTypeBuilder<OwnedCar> builder)
    {
        builder.ToTable("owned_cars");

        builder.Property(car => car.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(car => car.AccountId).HasColumnName("account_id");
        builder.Property(car => car.CarItemId).HasColumnName("car_item_id");

        builder.Property(car => car.Code)
            .HasColumnName("code")
            .HasMaxLength(InventoryConstants.CODE_MAX_LENGTH)
            .IsRequired();

        builder.Property(car => car.Color)
            .HasColumnName("color")
            .HasMaxLength(InventoryConstants.COLOR_MAX_LENGTH);

        builder.Property(car => car.Plate)
            .HasColumnName("plate")
            .HasMaxLength(InventoryConstants.PLATE_MAX_LENGTH);

        builder.Property(car => car.CreatedAt).HasColumnName("created_at");

        builder.HasKey(car => car.Id);

        builder.HasMany(car => car.Installations)
            .WithOne()
            .HasForeignKey(installation => installation.CarId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(car => car.Installations).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(car => car.AccountId).HasDatabaseName("ix_owned_cars_account");
    }
}
