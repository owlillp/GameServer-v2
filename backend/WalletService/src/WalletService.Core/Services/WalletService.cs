using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;
using WalletService.Contracts.Dtos;
using WalletService.Core.Abstractions;
using WalletService.Core.Features.Wallets.Models;
using WalletService.Core.Messaging.Publishers;
using WalletService.Domain.Wallets;
using WalletService.Domain.Wallets.Ledgers;

namespace WalletService.Core.Services;

public interface IWalletService
{
    Task<Result<WalletDto, Error>> CreditAsync(WalletOperation operation, CancellationToken cancellationToken);

    Task<Result<WalletDto, Error>> DebitAsync(WalletOperation operation, CancellationToken cancellationToken);
}

public sealed class WalletService(
    IReadDbContext dbContext,
    IWalletRepository walletRepository,
    ITransactionManager transactionManager,
    IWalletEventPublisher eventPublisher) : IWalletService
{
    public Task<Result<WalletDto, Error>> CreditAsync(WalletOperation operation, CancellationToken cancellationToken) =>
        WriteAsync(operation, LedgerDirection.Credit, cancellationToken);

    public Task<Result<WalletDto, Error>> DebitAsync(WalletOperation operation, CancellationToken cancellationToken) =>
        WriteAsync(operation, LedgerDirection.Debit, cancellationToken);

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

        var wallet = walletResult.Value;

        var entryResult = direction == LedgerDirection.Credit
            ? WriteCredit(wallet, operation)
            : WriteDebit(wallet, operation);

        if (entryResult.IsFailure)
        {
            return entryResult.Error;
        }

        var ledgerResult = await walletRepository.AddLedgerEntryAsync(entryResult.Value, cancellationToken);
        if (ledgerResult.IsFailure)
        {
            return ledgerResult.Error;
        }

        if (direction == LedgerDirection.Credit)
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

    private static Result<LedgerEntry, Error> WriteCredit(Wallet wallet, WalletOperation operation) =>
        wallet.Credit(
            operation.Currency,
            operation.Amount,
            operation.Reason,
            operation.OperationId,
            operation.ReferenceId,
            operation.ActorType,
            operation.ActorId,
            operation.Comment);

    private static Result<LedgerEntry, Error> WriteDebit(Wallet wallet, WalletOperation operation) =>
        wallet.Debit(
            operation.Currency,
            operation.Amount,
            operation.Reason,
            operation.OperationId,
            operation.ReferenceId,
            operation.ActorType,
            operation.ActorId,
            operation.Comment);
}