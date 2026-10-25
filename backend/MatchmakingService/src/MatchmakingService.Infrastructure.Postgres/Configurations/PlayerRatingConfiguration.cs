using MatchmakingService.Domain.Ratings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MatchmakingService.Infrastructure.Postgres.Configurations;

internal sealed class PlayerRatingConfiguration : IEntityTypeConfiguration<PlayerRating>
{
    public void Configure(EntityTypeBuilder<PlayerRating> builder)
    {
        builder.ToTable("player_ratings");

        builder.Property(rating => rating.AccountId).HasColumnName("account_id").ValueGeneratedNever();
        builder.Property(rating => rating.Rating).HasColumnName("rating");
        builder.Property(rating => rating.Games).HasColumnName("games");
        builder.Property(rating => rating.UpdatedAt).HasColumnName("updated_at");

        builder.HasKey(rating => rating.AccountId);
        builder.Property<uint>(Constants.ROW_VERSION).IsRowVersion();
    }
}
