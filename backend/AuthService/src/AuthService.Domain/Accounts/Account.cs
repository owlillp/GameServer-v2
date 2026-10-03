using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using Shared.SharedKernel.Errors;

namespace AuthService.Domain.Accounts;

public sealed class Account : IdentityUser<Guid>
{
    public string? DisplayName { get; private set; }

    public UserProfile Profile { get; private set; } = UserProfile.Empty;

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    // EF Core
    private Account() { }

    private Account(string email, string userName)
    {
        Id = Guid.CreateVersion7();
        Email = email;
        UserName = userName;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Profile = UserProfile.Empty;
    }

    public static Result<Account, Error> Create(string userName, string email)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return GeneralErrors.ValueIsRequired("userName");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return GeneralErrors.ValueIsRequired("email");
        }

        if (userName.Length > AccountConstants.USER_NAME_MAX_LENGTH)
        {
            return GeneralErrors.ValueIsInvalid("userName");
        }

        if (email.Length > AccountConstants.EMAIL_MAX_LENGTH)
        {
            return GeneralErrors.ValueIsInvalid("email");
        }

        return new Account(email, userName);
    }

    public UnitResult<Error> SetDisplayName(string? displayName)
    {
        if (displayName is { Length: > AccountConstants.USER_NAME_MAX_LENGTH })
        {
            return GeneralErrors.ValueIsInvalid("displayName");
        }

        DisplayName = displayName;
        UpdatedAt = DateTime.UtcNow;

        return UnitResult.Success<Error>();
    }
}
