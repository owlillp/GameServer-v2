using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace WalletService.Contracts.Communication;

public interface IWalletClient
{
    Task<Result<string, Error>> ReserveAsync(
        Guid walletId,
        string currency,
        long amount,
        Guid operationId,
        string? referenceId,
        CancellationToken cancellationToken);

    Task<Result<bool, Error>> CommitAsync(Guid holdId, CancellationToken cancellationToken);

    Task<Result<bool, Error>> ReleaseAsync(Guid holdId, CancellationToken cancellationToken);
}
