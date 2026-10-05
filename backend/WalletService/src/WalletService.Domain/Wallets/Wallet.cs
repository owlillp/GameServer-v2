using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;
using WalletService.Domain.Wallets.Ledgers;

namespace WalletService.Domain.Wallets;

public sealed class Wallet
{
    private readonly List<Balance> _balances = [];

    public Guid Id { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<Balance> Balances => _balances;

    // EF Core
    private Wallet() { }

    private Wallet(Guid accountId)
    {
        Id = accountId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Result<Wallet, Error> Create(Guid accountId)
    {
        if (accountId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("accountId");
        }

        return new Wallet(accountId);
    }

    public Result<LedgerEntry, Error> Credit(
        Currency currency,
        long amount,
        LedgerReason reason,
        Guid operationId,
        string? referenceId,
        LedgerActorType actorType,
        Guid? actorId,
        string? comment)
    {
        if (amount <= 0)
        {
            return WalletErrors.AmountMustBePositive;
        }

        GetOrCreateBalance(currency).Credit(amount);
        UpdatedAt = DateTime.UtcNow;

        return LedgerEntry.Create(
            Id,
            operationId,
            currency,
            LedgerDirection.Credit,
            amount,
            reason,
            referenceId,
            actorType,
            actorId,
            comment);
    }

    public Result<LedgerEntry, Error> Debit(
        Currency currency,
        long amount,
        LedgerReason reason,
        Guid operationId,
        string? referenceId,
        LedgerActorType actorType,
        Guid? actorId,
        string? comment)
    {
        if (amount <= 0)
        {
            return WalletErrors.AmountMustBePositive;
        }

        var balance = GetOrCreateBalance(currency);
        var debitResult = balance.Debit(amount);
        if (debitResult.IsFailure)
        {
            return debitResult.Error;
        }

        UpdatedAt = DateTime.UtcNow;

        return LedgerEntry.Create(
            Id,
            operationId,
            currency,
            LedgerDirection.Debit,
            amount,
            reason,
            referenceId,
            actorType,
            actorId,
            comment);
    }

    private Balance GetOrCreateBalance(Currency currency)
    {
        if(TryGetBalance(currency, out var balance))
        {
            return balance!;
        }

        var created = Balance.Empty(Id, currency);
        _balances.Add(created);
        UpdatedAt = DateTime.UtcNow;
        return created;
    }

    private bool TryGetBalance(Currency currency, out Balance? balance)
    {
        balance = _balances.FirstOrDefault(balance => string.Equals(balance.Currency.Code, currency.Code, StringComparison.Ordinal));
        return balance != null;
    }
}
