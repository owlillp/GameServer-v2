using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace ClanService.Domain.Clans;

/// <summary>
/// <see cref="MemberCount"/> and <see cref="ClanPoints"/> change by atomic SQL updates
/// (<c>member_count &lt; @max</c>, ledger + <c>clan_points + @amount</c>); the only EF write is
/// <see cref="Disband"/>. An atomic update also bumps the row version, so a loaded clan must not
/// be modified through EF after one in the same transaction.
/// </summary>
public sealed partial class Clan
{
    public Guid Id { get; private set; }

    public string Tag { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public Guid LeaderAccountId { get; private set; }

    public int MemberCount { get; private set; }

    public long ClanPoints { get; }

    public ClanStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? DisbandedAt { get; private set; }

    /// <summary>
    /// Wallet hold for the creation fee; committed right after creation, retried by a sweeper.
    /// </summary>
    public Guid? FeeHoldId { get; private set; }

    public DateTime? FeeCommittedAt { get; private set; }

    public bool IsActive => Status == ClanStatus.Active;

    // EF Core
    private Clan() { }

    public static Result<Clan, Error> Create(
        Guid id,
        string tag,
        string name,
        string? description,
        Guid leaderAccountId,
        Guid? feeHoldId,
        DateTime now)
    {
        var tagResult = NormalizeTag(tag);
        if (tagResult.IsFailure)
        {
            return tagResult.Error;
        }

        var infoResult = ValidateInfo(name, description);
        if (infoResult.IsFailure)
        {
            return infoResult.Error;
        }

        return new Clan
        {
            Id = id,
            Tag = tagResult.Value,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            LeaderAccountId = leaderAccountId,
            MemberCount = 1,
            Status = ClanStatus.Active,
            CreatedAt = now,
            FeeHoldId = feeHoldId,
            FeeCommittedAt = feeHoldId is null ? now : null,
        };
    }

    public static Result<string, Error> NormalizeTag(string? tag)
    {
        string normalized = (tag ?? string.Empty).Trim().ToUpperInvariant();

        return TagPattern().IsMatch(normalized) ? normalized : ClanErrors.TagInvalid;
    }

    public UnitResult<Error> UpdateInfo(string name, string? description)
    {
        var infoResult = ValidateInfo(name, description);
        if (infoResult.IsFailure)
        {
            return infoResult.Error;
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        return UnitResult.Success<Error>();
    }

    public void TransferLeadership(Guid newLeaderAccountId) => LeaderAccountId = newLeaderAccountId;

    public void MarkFeeCommitted(DateTime now) => FeeCommittedAt ??= now;

    public void Disband(DateTime now)
    {
        if (Status == ClanStatus.Disbanded)
        {
            return;
        }

        Status = ClanStatus.Disbanded;
        DisbandedAt = now;

        // Written through EF with the row version check: a join that raced the disband
        // (atomic slot update) changes the row version and makes one of them fail.
        MemberCount = 0;
    }

    private static UnitResult<Error> ValidateInfo(string? name, string? description)
    {
        string trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length is < ClanConstants.NAME_MIN_LENGTH or > ClanConstants.NAME_MAX_LENGTH)
        {
            return ClanErrors.NameInvalid;
        }

        if ((description?.Trim().Length ?? 0) > ClanConstants.DESCRIPTION_MAX_LENGTH)
        {
            return ClanErrors.DescriptionInvalid;
        }

        return UnitResult.Success<Error>();
    }

    [GeneratedRegex("^[A-Z0-9]{3,6}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex TagPattern();
}
