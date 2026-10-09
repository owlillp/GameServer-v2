using InventoryService.Contracts.Requests;
using Shared.Core.Abstractions;

namespace InventoryService.Core.Features.Inventories.Commands.RevokeItems;

public sealed record RevokeItemsCommand(Guid AccountId, RevokeItemsRequest Request) : ICommand;
