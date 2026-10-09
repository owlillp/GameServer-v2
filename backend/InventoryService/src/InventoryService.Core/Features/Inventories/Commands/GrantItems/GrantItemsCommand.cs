using InventoryService.Contracts.Requests;
using Shared.Core.Abstractions;

namespace InventoryService.Core.Features.Inventories.Commands.GrantItems;

public sealed record GrantItemsCommand(Guid AccountId, GrantItemsRequest Request) : ICommand;
