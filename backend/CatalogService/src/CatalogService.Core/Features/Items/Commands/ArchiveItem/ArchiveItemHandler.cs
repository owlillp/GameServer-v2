using CatalogService.Contracts.Dtos;
using CatalogService.Core.Abstractions;
using CatalogService.Core.Cache;
using CatalogService.Core.Features.Items.Models;
using CatalogService.Domain.Items;
using CSharpFunctionalExtensions;
using FluentValidation;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;

namespace CatalogService.Core.Features.Items.Commands.ArchiveItem;

public sealed class ArchiveItemHandler(
    IValidator<ArchiveItemCommand> validator,
    IItemRepository itemRepository,
    ITransactionManager transactionManager,
    CatalogCacheInvalidator cacheInvalidator) : ICommandHandler<ItemDetailDto, ArchiveItemCommand>
{
    public async Task<Result<ItemDetailDto, Error>> Handle(
        ArchiveItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        var itemResult = await itemRepository.GetByIdAsync(command.ItemId, cancellationToken);
        if (itemResult.IsFailure)
        {
            return itemResult.Error;
        }

        Item item = itemResult.Value;

        var archiveResult = item.Archive();
        if (archiveResult.IsFailure)
        {
            return archiveResult.Error;
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        await cacheInvalidator.InvalidateItemAsync(item.Id, cancellationToken);

        return ItemMapper.ToDetailDto(item);
    }
}
