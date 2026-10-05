using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;
using WalletService.Contracts.Dtos;
using WalletService.Core.Abstractions;
using WalletService.Core.Messaging.Publishers;
using WalletService.Domain.Wallets;
using WalletService.Domain.Wallets.Ledgers;

namespace WalletService.Core.Features.Wallets;

public sealed record WalletOperation(
    Guid WalletId,
    Currency Currency,
    long Amount,
    LedgerReason Reason,
    Guid OperationId,
    string? ReferenceId,
    LedgerActorType ActorType,
    Guid? ActorId,
    string? Comment);

/// <summary>
/// Единая точка записи денежных операций: идемпотентность, транзакция,
/// ledger-проводка, outbox-событие. Переиспользуется машинными (service)
/// и админскими сценариями — отличается только actor.
/// </summary>
public sealed class WalletWriter(
    IReadDbContext dbContext,
    IWalletRepository walletRepository,
    ITransactionManager transactionManager,
    IWalletEventPublisher eventPublisher)
{
    public Task<Result<WalletDto, Error>> TopUpAsync(WalletOperation operation, CancellationToken cancellationToken) =>
        WriteAsync(operation, LedgerDirection.TOP_UP, cancellationToken);

    public Task<Result<WalletDto, Error>> SpentAsync(WalletOperation operation, CancellationToken cancellationToken) =>
        WriteAsync(operation, LedgerDirection.SPENT, cancellationToken);

    private async Task<Result<WalletDto, Error>> WriteAsync(
        WalletOperation operation,
        LedgerDirection direction,
        CancellationToken cancellationToken)
    {
        bool alreadyProcessed = await dbContext.LedgerRead.AnyAsync(
            entry => entry.WalletId == operation.WalletId
                && entry.OperationId == operation.OperationId
                && entry.Direction == direction,
            cancellationToken);

        if (alreadyProcessed)
        {
            return WalletErrors.OperationAlreadyProcessed;
        }

        bool currencyAvailable = await dbContext.CurrenciesRead.AnyAsync(
            currency => currency.Code == operation.Currency.Code && currency.IsActive,
            cancellationToken);

        if (!currencyAvailable)
        {
            return WalletErrors.CurrencyUnavailable;
        }

        var transactionResult = await transactionManager.BeginTransactionAsync(cancellationToken);
        if (transactionResult.IsFailure)
        {
            return transactionResult.Error;
        }

        var walletResult = await walletRepository.GetByAsync(
            wallet => wallet.Id == operation.WalletId,
            cancellationToken);

        if (walletResult.IsFailure)
        {
            return walletResult.Error;
        }

        Wallet wallet = walletResult.Value;

        var entryResult = direction == LedgerDirection.TOP_UP
            ? WriteTopUp(wallet, operation)
            : WriteSpent(wallet, operation);

        if (entryResult.IsFailure)
        {
            return entryResult.Error;
        }

        var ledgerResult = await walletRepository.AddLedgerEntryAsync(entryResult.Value, cancellationToken);
        if (ledgerResult.IsFailure)
        {
            return ledgerResult.Error;
        }

        if (direction == LedgerDirection.TOP_UP)
        {
            await eventPublisher.PublishCreditedAsync(
                wallet.Id,
                operation.Currency.Code,
                operation.Amount,
                operation.Reason,
                operation.OperationId);
        }
        else
        {
            await eventPublisher.PublishDebitedAsync(
                wallet.Id,
                operation.Currency.Code,
                operation.Amount,
                operation.Reason,
                operation.OperationId);
        }

        var commitResult = await transactionManager.CommitTransactionAsync(cancellationToken);
        if (commitResult.IsFailure)
        {
            return commitResult.Error;
        }

        return WalletMapper.ToDto(wallet);
    }

    private static Result<LedgerEntry, Error> WriteTopUp(Wallet wallet, WalletOperation operation) =>
        wallet.TopUp(
            operation.Currency,
            operation.Amount,
            operation.Reason,
            operation.OperationId,
            operation.ReferenceId,
            operation.ActorType,
            operation.ActorId,
            operation.Comment);

    private static Result<LedgerEntry, Error> WriteSpent(Wallet wallet, WalletOperation operation) =>
        wallet.Spent(
            operation.Currency,
            operation.Amount,
            operation.Reason,
            operation.OperationId,
            operation.ReferenceId,
            operation.ActorType,
            operation.ActorId,
            operation.Comment);
}
