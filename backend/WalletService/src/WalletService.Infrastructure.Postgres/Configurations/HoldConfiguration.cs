using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WalletService.Domain.Wallets;
using WalletService.Domain.Wallets.Holds;

namespace WalletService.Infrastructure.Postgres.Configurations;

internal sealed class HoldConfiguration : IEntityTypeConfiguration<Hold>
{
    public void Configure(EntityTypeBuilder<Hold> builder)
    {
        builder.ToTable("holds");

        builder.Property(hold => hold.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(hold => hold.WalletId).HasColumnName("wallet_id");
        builder.Property(hold => hold.OperationId).HasColumnName("operation_id");

        builder.Property(hold => hold.Currency)
            .HasColumnName("currency")
            .HasConversion(currency => currency.Code, code => Currency.Create(code).Value)
            .HasMaxLength(Currency.MAX_LENGTH);

        builder.Property(hold => hold.Amount).HasColumnName("amount");

        builder.Property(hold => hold.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(hold => hold.ReferenceId)
            .HasColumnName("reference_id")
            .HasMaxLength(WalletConstants.REFERENCE_MAX_LENGTH);

        builder.Property(hold => hold.ExpiresAt).HasColumnName("expires_at");
        builder.Property(hold => hold.CreatedAt).HasColumnName("created_at");
        builder.Property(hold => hold.UpdatedAt).HasColumnName("updated_at");

        builder.HasKey(hold => hold.Id);

        builder.HasIndex(hold => new { hold.WalletId, hold.OperationId })
            .IsUnique()
            .HasDatabaseName("uq_holds_wallet_operation");

        builder.HasIndex(hold => hold.ExpiresAt)
            .HasDatabaseName("ix_holds_expires_at");
    }
}
