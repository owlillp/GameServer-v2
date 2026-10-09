using Shared.Core.Abstractions;

namespace InventoryService.Core.Features.Inventories.Queries.GetInventory;

public sealed record GetInventoryQuery(Guid AccountId) : IQuery;
