using Shared.Core.Abstractions;

namespace TemplateService.Core.Features.Items.Commands.DeleteItem;

public sealed record DeleteItemCommand(Guid ItemId) : ICommand;
