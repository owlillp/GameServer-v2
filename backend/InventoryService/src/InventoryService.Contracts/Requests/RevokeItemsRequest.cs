namespace InventoryService.Contracts.Requests;

public sealed record RevokeItemsRequest(IReadOnlyList<Guid> ItemIds);
