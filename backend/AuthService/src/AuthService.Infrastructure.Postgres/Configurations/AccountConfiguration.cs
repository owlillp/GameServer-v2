using AuthService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Infrastructure.Postgres.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(account => account.Id);

        builder.Property(account => account.Id).HasColumnName("id");

        builder.Property(account => account.Email)
            .HasColumnName("email")
            .HasMaxLength(AccountConstants.EMAIL_MAX_LENGTH)
            .IsRequired();

        builder.Property(account => account.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(account => account.Email).IsUnique();
    }
}
