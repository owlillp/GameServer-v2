using ClanService.Domain;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace ClanService.Core.Services;

/// <summary>
/// Turns unique-index violations from a lost race into the business error the user should see.
/// </summary>
public static class ClanSaveErrors
{
    public const string TAG_INDEX = "uq_clans_tag";
    public const string MEMBER_KEY = "pk_clan_members";
    public const string PENDING_REQUEST_INDEX = "uq_clan_join_requests_pending";

    public static Error Map(Error error)
    {
        if (!error.IsUniqueViolation())
        {
            return error;
        }

        string text = error.GetMessage();

        if (text.Contains(TAG_INDEX, StringComparison.Ordinal))
        {
            return ClanErrors.TagTaken;
        }

        return text.Contains(PENDING_REQUEST_INDEX, StringComparison.Ordinal)
            ? ClanErrors.RequestDuplicate
            : ClanErrors.AlreadyInClan;
    }
}
