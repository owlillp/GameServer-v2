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

namespace CatalogService.Core.Features.Items.Commands.ReactivateItem;

public sealed class ReactivateItemHandler(
    IValidator<ReactivateItemCommand> validator,
    IItemRepository itemRepository,
    ITransactionManager transactionManager,
    CatalogCacheInvalidator cacheInvalidator) : ICommandHandler<ItemDetailDto, ReactivateItemCommand>
{
    public async Task<Result<ItemDetailDto, Error>> Handle(
        ReactivateItemCommand command,
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

        var reactivateResult = item.Reactivate();
        if (reactivateResult.IsFailure)
        {
            return reactivateResult.Error;
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
