using CatalogService.Contracts.Requests;
using Shared.Core.Abstractions;

namespace CatalogService.Core.Features.Items.Commands.UpdateItem;

public sealed record UpdateItemCommand(Guid ItemId, UpdateItemRequest Request) : ICommand;
