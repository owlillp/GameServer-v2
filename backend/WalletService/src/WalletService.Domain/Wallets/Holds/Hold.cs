using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace WalletService.Domain.Wallets.Holds;

public sealed class Hold
{
    public Guid Id { get; private set; }

    public Guid WalletId { get; private set; }

    public Guid OperationId { get; private set; }

    public Currency Currency { get; private set; } = null!;

    public long Amount { get; private set; }

    public HoldStatus Status { get; private set; }

    public string? ReferenceId { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    // EF Core
    private Hold() { }

    private Hold(
        Guid walletId,
        Guid operationId,
        Currency currency,
        long amount,
        string? referenceId,
        DateTime expiresAt)
    {
        Id = Guid.CreateVersion7();
        WalletId = walletId;
        OperationId = operationId;
        Currency = currency;
        Amount = amount;
        Status = HoldStatus.HELD;
        ReferenceId = referenceId;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static Result<Hold, Error> Create(
        Guid walletId,
        Guid operationId,
        Currency currency,
        long amount,
        string? referenceId,
        DateTime expiresAt)
    {
        if (walletId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("walletId");
        }

        if (operationId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("operationId");
        }

        if (amount <= 0)
        {
            return WalletErrors.AmountMustBePositive;
        }

        return new Hold(walletId, operationId, currency, amount, referenceId, expiresAt);
    }

    public UnitResult<Error> Commit()
    {
        if (Status == HoldStatus.COMMITTED)
        {
            return UnitResult.Success<Error>();
        }

        if (Status == HoldStatus.RELEASED)
        {
            return WalletErrors.HoldAlreadyReleased;
        }

        Status = HoldStatus.COMMITTED;
        UpdatedAt = DateTime.UtcNow;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Release()
    {
        if (Status == HoldStatus.RELEASED)
        {
            return UnitResult.Success<Error>();
        }

        if (Status == HoldStatus.COMMITTED)
        {
            return WalletErrors.HoldAlreadyCommitted;
        }

        Status = HoldStatus.RELEASED;
        UpdatedAt = DateTime.UtcNow;
        return UnitResult.Success<Error>();
    }
}
