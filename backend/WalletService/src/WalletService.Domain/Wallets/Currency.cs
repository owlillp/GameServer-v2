using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace WalletService.Domain.Wallets;

public sealed record Currency
{
    public const int MAX_LENGTH = 16;

    public string Code { get; }

    private Currency(string code) => Code = code;

    public static Result<Currency, Error> Create(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return GeneralErrors.ValueIsRequired("currency");
        }

        string normalized = code.Trim().ToUpperInvariant();
        if (normalized.Length > MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("currency", max: MAX_LENGTH);
        }

        return new Currency(normalized);
    }

    public override string ToString() => Code;
}
