using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace WalletService.Domain.Wallets.Ledgers;

/// <summary>
/// Append-only запись журнала операций (ledger). Баланс — производная величина,
/// которую всегда можно пересчитать из проводок. Записи не изменяются и не удаляются.
/// Несёт аудит-провенанс: кто (actor) и зачем (comment) выполнил операцию.
/// </summary>
public sealed class LedgerEntry
{
    public Guid Id { get; private set; }

    public Guid WalletId { get; private set; }

    public Guid OperationId { get; private set; }

    public Currency Currency { get; private set; } = null!;

    public LedgerDirection Direction { get; private set; }

    public long Amount { get; private set; }

    public LedgerReason Reason { get; private set; }

    public string? ReferenceId { get; private set; }

    public LedgerActorType ActorType { get; private set; }

    public Guid? ActorId { get; private set; }

    public string? Comment { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // EF Core
    private LedgerEntry() { }

    private LedgerEntry(
        Guid walletId,
        Guid operationId,
        Currency currency,
        LedgerDirection direction,
        long amount,
        LedgerReason reason,
        string? referenceId,
        LedgerActorType actorType,
        Guid? actorId,
        string? comment)
    {
        Id = Guid.CreateVersion7();
        WalletId = walletId;
        OperationId = operationId;
        Currency = currency;
        Direction = direction;
        Amount = amount;
        Reason = reason;
        ReferenceId = referenceId;
        ActorType = actorType;
        ActorId = actorId;
        Comment = comment;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<LedgerEntry, Error> Create(
        Guid walletId,
        Guid operationId,
        Currency currency,
        LedgerDirection direction,
        long amount,
        LedgerReason reason,
        string? referenceId,
        LedgerActorType actorType,
        Guid? actorId,
        string? comment)
    {
        if (walletId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("walletId");
        }

        if (amount <= 0)
        {
            return WalletErrors.AmountMustBePositive;
        }

        return new LedgerEntry(
            walletId,
            operationId,
            currency,
            direction,
            amount,
            reason,
            referenceId,
            actorType,
            actorId,
            comment);
    }
}
