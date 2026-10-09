using Shared.Core.Abstractions;

namespace InventoryService.Core.Features.Inventories.Commands.EquipOutfit;

public sealed record EquipOutfitCommand(Guid AccountId, string Slot, Guid ItemId) : ICommand;
