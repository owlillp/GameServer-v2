using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;
using WalletService.Domain.Wallets;

namespace WalletService.Core.Features.Currencies.Models;

internal static class CurrencyKindParser
{
    public static Result<CurrencyKind, Error> Parse(string value)
    {
        if (Enum.TryParse(value, ignoreCase: true, out CurrencyKind kind)
            && Enum.IsDefined(kind))
        {
            return kind;
        }

        return GeneralErrors.ValueIsInvalid("kind");
    }
}
