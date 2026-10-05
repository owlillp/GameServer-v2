using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace WalletService.Domain.Wallets;

public sealed class Balance
{
    public Guid WalletId { get; private set; }

    public Currency Currency { get; private set; } = null!;

    public long Available { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    // EF Core
    private Balance() { }

    private Balance(Guid walletId, Currency currency)
    {
        WalletId = walletId;
        Currency = currency;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    internal static Balance Empty(Guid walletId, Currency currency) => new(walletId, currency);

    internal void TopUp(long amount)
    {
        Available += amount;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    internal UnitResult<Error> Spent(long amount)
    {
        if (amount <= 0)
        {
            return WalletErrors.AmountMustBePositive;
        }

        if (Available < amount)
        {
            return WalletErrors.InsufficientFunds;
        }

        Available -= amount;
        UpdatedAt = DateTimeOffset.UtcNow;
        return UnitResult.Success<Error>();
    }
}
