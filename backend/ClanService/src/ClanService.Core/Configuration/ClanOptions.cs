using System.ComponentModel.DataAnnotations;

namespace ClanService.Core.Configuration;

public sealed class ClanOptions
{
    public const string SECTION_NAME = "Clans";

    [Range(2, 500)]
    public int MaxMembers { get; init; } = 50;

    /// <summary>
    /// After leaving a clan on their own a player cannot join another one for this long.
    /// </summary>
    [Range(0, 24 * 30)]
    public int JoinCooldownHours { get; init; } = 24;

    /// <summary>
    /// Price of creating a clan; 0 makes it free.
    /// </summary>
    [Range(0, long.MaxValue)]
    public long CreationFee { get; init; }

    [Required]
    [MaxLength(16)]
    public string CreationFeeCurrency { get; init; } = "coins";

    [Range(1, 3600)]
    public int FeeSweepIntervalSeconds { get; init; } = 60;

    [Range(1, 200)]
    public int SearchLimit { get; init; } = 50;
}
