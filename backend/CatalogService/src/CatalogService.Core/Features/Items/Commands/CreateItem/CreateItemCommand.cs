using CatalogService.Contracts.Requests;
using Shared.Core.Abstractions;

namespace CatalogService.Core.Features.Items.Commands.CreateItem;

public sealed record CreateItemCommand(CreateItemRequest Request) : ICommand;
