using Shared.Core.Abstractions;

namespace CatalogService.Core.Features.Items.Commands.ReactivateItem;

public sealed record ReactivateItemCommand(Guid ItemId) : ICommand;
