using Shared.Core.Abstractions;

namespace InventoryService.Core.Features.Inventories.Commands.RemovePart;

public sealed record RemovePartCommand(Guid AccountId, Guid CarId, string Slot) : ICommand;
