using InventoryService.Domain.Inventories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Infrastructure.Postgres.Configurations;

internal sealed class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("inventories");

        builder.Property(inventory => inventory.AccountId)
            .HasColumnName("account_id")
            .ValueGeneratedNever();

        builder.Property(inventory => inventory.CreatedAt).HasColumnName("created_at");
        builder.Property(inventory => inventory.UpdatedAt).HasColumnName("updated_at");

        builder.HasKey(inventory => inventory.AccountId);

        builder.HasMany(inventory => inventory.Items)
            .WithOne()
            .HasForeignKey(item => item.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(inventory => inventory.Outfit)
            .WithOne()
            .HasForeignKey(entry => entry.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(inventory => inventory.Cars)
            .WithOne()
            .HasForeignKey(car => car.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(inventory => inventory.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(inventory => inventory.Outfit).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(inventory => inventory.Cars).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
