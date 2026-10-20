using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;
using WalletService.Contracts.Dtos;
using WalletService.Core.Abstractions;
using WalletService.Core.Features.Wallets.Models;
using WalletService.Core.Messaging.Publishers;
using WalletService.Domain.Wallets;
using WalletService.Domain.Wallets.Holds;
using WalletService.Domain.Wallets.Ledgers;

namespace WalletService.Core.Services;

public interface IWalletService
{
    Task<Result<WalletDto, Error>> CreditAsync(WalletOperation operation, CancellationToken cancellationToken);

    Task<Result<WalletDto, Error>> DebitAsync(WalletOperation operation, CancellationToken cancellationToken);

    Task<Result<Hold, Error>> ReserveAsync(
        WalletOperation operation,
        TimeSpan lifetime,
        CancellationToken cancellationToken);

    Task<Result<Hold, Error>> CommitAsync(Guid holdId, CancellationToken cancellationToken);

    Task<Result<Hold, Error>> ReleaseAsync(Guid holdId, CancellationToken cancellationToken);
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

    public async Task<Result<Hold, Error>> ReserveAsync(
        WalletOperation operation,
        TimeSpan lifetime,
        CancellationToken cancellationToken)
    {
        Hold? existing = await dbContext.HoldsRead.FirstOrDefaultAsync(
            hold => hold.WalletId == operation.WalletId && hold.OperationId == operation.OperationId,
            cancellationToken);

        if (existing is not null)
        {
            return existing.Status == HoldStatus.RELEASED
                ? WalletErrors.HoldAlreadyReleased
                : existing;
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
        var debitResult = wallet.Debit(
            operation.Currency,
            operation.Amount,
            operation.Reason,
            operation.OperationId,
            operation.ReferenceId,
            operation.ActorType,
            operation.ActorId,
            operation.Comment);

        if (debitResult.IsFailure)
        {
            return debitResult.Error;
        }

        var ledgerResult = await walletRepository.AddLedgerEntryAsync(debitResult.Value, cancellationToken);
        if (ledgerResult.IsFailure)
        {
            return ledgerResult.Error;
        }

        var holdResult = Hold.Create(
            operation.WalletId,
            operation.OperationId,
            operation.Currency,
            operation.Amount,
            operation.ReferenceId,
            DateTime.UtcNow.Add(lifetime));

        if (holdResult.IsFailure)
        {
            return holdResult.Error;
        }

        await walletRepository.AddHoldAsync(holdResult.Value, cancellationToken);

        await eventPublisher.PublishDebitedAsync(
            wallet.Id,
            operation.Currency.Code,
            operation.Amount,
            operation.Reason,
            operation.OperationId);

        var commitResult = await transactionManager.CommitTransactionAsync(cancellationToken);
        if (commitResult.IsFailure)
        {
            return commitResult.Error;
        }

        return holdResult.Value;
    }

    public async Task<Result<Hold, Error>> CommitAsync(Guid holdId, CancellationToken cancellationToken)
    {
        var holdResult = await walletRepository.GetHoldAsync(holdId, cancellationToken);
        if (holdResult.IsFailure)
        {
            return holdResult.Error;
        }

        var hold = holdResult.Value;
        var commitResult = hold.Commit();
        if (commitResult.IsFailure)
        {
            return commitResult.Error;
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? saveResult.Error : hold;
    }

    public async Task<Result<Hold, Error>> ReleaseAsync(Guid holdId, CancellationToken cancellationToken)
    {
        var holdResult = await walletRepository.GetHoldAsync(holdId, cancellationToken);
        if (holdResult.IsFailure)
        {
            return holdResult.Error;
        }

        var hold = holdResult.Value;
        if (hold.Status == HoldStatus.RELEASED)
        {
            return hold;
        }

        var releaseResult = hold.Release();
        if (releaseResult.IsFailure)
        {
            return releaseResult.Error;
        }

        var transactionResult = await transactionManager.BeginTransactionAsync(cancellationToken);
        if (transactionResult.IsFailure)
        {
            return transactionResult.Error;
        }

        var walletResult = await walletRepository.GetByAsync(
            wallet => wallet.Id == hold.WalletId,
            cancellationToken);

        if (walletResult.IsFailure)
        {
            return walletResult.Error;
        }

        var creditResult = walletResult.Value.Credit(
            hold.Currency,
            hold.Amount,
            LedgerReason.Compensation,
            hold.OperationId,
            hold.Id.ToString(),
            LedgerActorType.Service,
            actorId: null,
            comment: "Hold released");

        if (creditResult.IsFailure)
        {
            return creditResult.Error;
        }

        var ledgerResult = await walletRepository.AddLedgerEntryAsync(creditResult.Value, cancellationToken);
        if (ledgerResult.IsFailure)
        {
            return ledgerResult.Error;
        }

        await eventPublisher.PublishCreditedAsync(
            hold.WalletId,
            hold.Currency.Code,
            hold.Amount,
            LedgerReason.Compensation,
            hold.OperationId);

        var commitResult = await transactionManager.CommitTransactionAsync(cancellationToken);
        return commitResult.IsFailure ? commitResult.Error : hold;
    }

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