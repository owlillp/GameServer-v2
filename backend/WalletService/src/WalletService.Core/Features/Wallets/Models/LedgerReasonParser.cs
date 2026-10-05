using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;
using WalletService.Domain.Wallets.Ledgers;

namespace WalletService.Core.Features.Wallets.Models;

internal static class LedgerReasonParser
{
    public static Result<LedgerReason, Error> Parse(string value)
    {
        if (Enum.TryParse(value, ignoreCase: true, out LedgerReason reason)
            && Enum.IsDefined(reason))
        {
            return reason;
        }

        return GeneralErrors.ValueIsInvalid("reason");
    }
}
