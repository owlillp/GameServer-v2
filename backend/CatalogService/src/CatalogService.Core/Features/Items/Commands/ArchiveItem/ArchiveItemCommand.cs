using Shared.Core.Abstractions;

namespace CatalogService.Core.Features.Items.Commands.ArchiveItem;

public sealed record ArchiveItemCommand(Guid ItemId) : ICommand;
