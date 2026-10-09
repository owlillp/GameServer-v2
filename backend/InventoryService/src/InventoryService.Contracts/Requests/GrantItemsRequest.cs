namespace InventoryService.Contracts.Requests;

public sealed record GrantItemsRequest(
    IReadOnlyList<GrantItemRequest> Items,
    string Source,
    Guid? OperationId,
    string? ReferenceId);
