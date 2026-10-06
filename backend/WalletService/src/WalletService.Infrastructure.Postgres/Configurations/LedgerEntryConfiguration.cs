using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WalletService.Domain.Wallets;
using WalletService.Domain.Wallets.Ledgers;

namespace WalletService.Infrastructure.Postgres.Configurations;

internal sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("ledger_entries");

        builder.Property(entry => entry.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(entry => entry.WalletId).HasColumnName("wallet_id");
        builder.Property(entry => entry.OperationId).HasColumnName("operation_id");

        builder.Property(entry => entry.Currency)
            .HasColumnName("currency")
            .HasConversion(currency => currency.Code, code => Currency.Create(code).Value)
            .HasMaxLength(Currency.MAX_LENGTH);

        builder.Property(entry => entry.Direction)
            .HasColumnName("direction")
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(entry => entry.Amount).HasColumnName("amount");

        builder.Property(entry => entry.Reason)
            .HasColumnName("reason")
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(entry => entry.ReferenceId)
            .HasColumnName("reference_id")
            .HasMaxLength(WalletConstants.REFERENCE_MAX_LENGTH);

        builder.Property(entry => entry.ActorType)
            .HasColumnName("actor_type")
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(entry => entry.ActorId).HasColumnName("actor_id");

        builder.Property(entry => entry.Comment)
            .HasColumnName("comment")
            .HasMaxLength(WalletConstants.COMMENT_MAX_LENGTH);

        builder.Property(entry => entry.CreatedAt).HasColumnName("created_at");

        builder.HasKey(entry => entry.Id);

        builder.HasIndex(entry => new { entry.WalletId, entry.OperationId, entry.Direction })
            .IsUnique()
            .HasDatabaseName("uq_ledger_wallet_operation_direction");

        builder.HasIndex(entry => new { entry.WalletId, entry.CreatedAt })
            .HasDatabaseName("ix_ledger_wallet_created_at");

        builder.HasIndex(entry => new { entry.ActorType, entry.ActorId })
            .HasDatabaseName("ix_ledger_actor");
    }
}
