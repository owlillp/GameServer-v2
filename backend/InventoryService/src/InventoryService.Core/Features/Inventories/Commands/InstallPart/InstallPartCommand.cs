using Shared.Core.Abstractions;

namespace InventoryService.Core.Features.Inventories.Commands.InstallPart;

public sealed record InstallPartCommand(Guid AccountId, Guid CarId, string Slot, Guid PartItemId) : ICommand;
