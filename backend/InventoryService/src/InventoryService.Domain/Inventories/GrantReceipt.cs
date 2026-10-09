using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace InventoryService.Domain.Inventories;

public sealed class GrantReceipt
{
    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public Guid OperationId { get; private set; }

    public string Source { get; private set; } = null!;

    public int ItemCount { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // EF Core
    private GrantReceipt() { }

    private GrantReceipt(Guid accountId, Guid operationId, string source, int itemCount)
    {
        Id = Guid.CreateVersion7();
        AccountId = accountId;
        OperationId = operationId;
        Source = source;
        ItemCount = itemCount;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<GrantReceipt, Error> Create(Guid accountId, Guid operationId, string source, int itemCount)
    {
        if (accountId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("accountId");
        }

        if (operationId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("operationId");
        }

        return new GrantReceipt(accountId, operationId, source, itemCount);
    }
}
