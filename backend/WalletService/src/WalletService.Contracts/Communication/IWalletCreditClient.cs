using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace WalletService.Contracts.Communication;

public interface IWalletCreditClient
{
    /// <summary>
    /// Idempotent by <paramref name="operationId"/>: a repeated call with the same id succeeds
    /// without crediting twice.
    /// </summary>
    Task<Result<bool, Error>> CreditAsync(
        Guid accountId,
        string currency,
        long amount,
        Guid operationId,
        string reason,
        string? referenceId,
        CancellationToken cancellationToken);

    /// <summary>
    /// True if a credit with this operation id exists in the wallet ledger (reconciliation).
    /// </summary>
    Task<Result<bool, Error>> OperationExistsAsync(Guid accountId, Guid operationId, CancellationToken cancellationToken);
}
