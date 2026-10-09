using Shared.Core.Abstractions;

namespace InventoryService.Core.Features.Inventories.Commands.RemoveCar;

public sealed record RemoveCarCommand(Guid AccountId, Guid CarId) : ICommand;
