using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Contracts.Communication;

public interface IWalletCreditClient
{
    Task<Result<bool, Error>> CreditAsync(
        Guid accountId,
        string currency,
        long amount,
        Guid operationId,
        string? referenceId,
        CancellationToken cancellationToken);
}
