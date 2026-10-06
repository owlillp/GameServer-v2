using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace WalletService.Domain.Wallets;

public sealed class CurrencyDefinition
{
    public string Code { get; private set; } = null!;

    public string DisplayName { get; private set; } = null!;

    public CurrencyKind Kind { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    // EF Core
    private CurrencyDefinition() { }

    private CurrencyDefinition(string code, string displayName, CurrencyKind kind)
    {
        Code = code;
        DisplayName = displayName;
        Kind = kind;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static Result<CurrencyDefinition, Error> Create(string code, string displayName, CurrencyKind kind)
    {
        var currencyResult = Currency.Create(code);
        if (currencyResult.IsFailure)
        {
            return currencyResult.Error;
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return GeneralErrors.ValueIsRequired("displayName");
        }

        string trimmedName = displayName.Trim();
        if (trimmedName.Length > WalletConstants.CURRENCY_DISPLAY_NAME_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid(
                "displayName",
                max: WalletConstants.CURRENCY_DISPLAY_NAME_MAX_LENGTH);
        }

        return new CurrencyDefinition(currencyResult.Value.Code, trimmedName, kind);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
