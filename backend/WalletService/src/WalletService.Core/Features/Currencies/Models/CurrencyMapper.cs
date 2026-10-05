using WalletService.Contracts.Dtos;
using WalletService.Domain.Wallets;

namespace WalletService.Core.Features.Currencies.Models;

internal static class CurrencyMapper
{
    public static CurrencyDto ToDto(CurrencyDefinition currency) =>
        new(
            currency.Code,
            currency.DisplayName,
            currency.Kind.ToString(),
            currency.IsActive,
            currency.CreatedAt);
}
