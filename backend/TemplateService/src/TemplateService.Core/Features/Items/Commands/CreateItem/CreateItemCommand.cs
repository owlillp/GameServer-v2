using TemplateService.Contracts.Items;
using Shared.Core.Abstractions;

namespace TemplateService.Core.Features.Items.Commands.CreateItem;

public sealed record CreateItemCommand(CreateItemRequest Request) : ICommand;
