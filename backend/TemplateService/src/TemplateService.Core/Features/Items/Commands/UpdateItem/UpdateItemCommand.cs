using TemplateService.Contracts.Items;
using Shared.Core.Abstractions;

namespace TemplateService.Core.Features.Items.Commands.UpdateItem;

public sealed record UpdateItemCommand(Guid ItemId, UpdateItemRequest Request) : ICommand;
