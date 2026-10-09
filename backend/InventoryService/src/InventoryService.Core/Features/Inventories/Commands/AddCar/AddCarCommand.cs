using InventoryService.Contracts.Requests;
using Shared.Core.Abstractions;

namespace InventoryService.Core.Features.Inventories.Commands.AddCar;

public sealed record AddCarCommand(Guid AccountId, AddCarRequest Request) : ICommand;
