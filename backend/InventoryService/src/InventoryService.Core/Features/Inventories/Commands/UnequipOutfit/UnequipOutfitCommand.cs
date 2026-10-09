using Shared.Core.Abstractions;

namespace InventoryService.Core.Features.Inventories.Commands.UnequipOutfit;

public sealed record UnequipOutfitCommand(Guid AccountId, string Slot) : ICommand;
