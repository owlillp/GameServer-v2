using Shared.SharedKernel.Errors;

namespace WalletService.Domain.Wallets;

public static class WalletErrors
{
    public static Error AmountMustBePositive { get; } =
        Error.Validation("wallet.amount.not_positive", "Amount must be greater than zero", "amount");

    public static Error InsufficientFunds { get; } =
        Error.Validation("wallet.insufficient_funds", "Insufficient funds", "amount");

    public static Error CurrencyUnavailable { get; } =
        Error.Validation("wallet.currency.unavailable", "Currency is unknown or disabled", "currency");

    public static Error OperationAlreadyProcessed { get; } =
        Error.Conflict("wallet.operation.duplicate", "Operation has already been processed", "operationId");
}
