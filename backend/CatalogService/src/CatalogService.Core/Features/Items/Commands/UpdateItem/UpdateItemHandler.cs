using CatalogService.Contracts.Dtos;
using CatalogService.Contracts.Requests;
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

namespace CatalogService.Core.Features.Items.Commands.UpdateItem;

public sealed class UpdateItemHandler(
    IValidator<UpdateItemCommand> validator,
    IItemRepository itemRepository,
    ITransactionManager transactionManager,
    CatalogCacheInvalidator cacheInvalidator) : ICommandHandler<ItemDetailDto, UpdateItemCommand>
{
    public async Task<Result<ItemDetailDto, Error>> Handle(
        UpdateItemCommand command,
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
        UpdateItemRequest request = command.Request;

        var rarityResult = ItemRarityParser.Parse(request.Rarity);
        if (rarityResult.IsFailure)
        {
            return rarityResult.Error;
        }

        var updateResult = item.Update(
            request.DisplayName,
            request.Description,
            rarityResult.Value,
            request.ModelCode,
            request.Slot,
            request.Tags,
            request.FitTags);

        if (updateResult.IsFailure)
        {
            return updateResult.Error;
        }

        item.ClearStats();
        item.ClearPrices();
        item.ClearAvailability();
        ApplyRelations(item, request);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        await cacheInvalidator.InvalidateItemAsync(item.Id, cancellationToken);

        return ItemMapper.ToDetailDto(item);
    }

    private static void ApplyRelations(Item item, UpdateItemRequest request)
    {
        if (request.Stats is not null)
        {
            foreach (ItemStatRequest stat in request.Stats)
            {
                item.SetStat(stat.Key, stat.Value);
            }
        }

        if (request.Prices is not null)
        {
            foreach (ItemPriceRequest price in request.Prices)
            {
                item.SetPrice(price.CurrencyCode, price.Amount, price.IsActive, price.ValidFrom);
            }
        }

        if (request.Availability is not null)
        {
            foreach (AvailabilityWindowRequest window in request.Availability)
            {
                item.SetAvailability(window.StartsAt, window.EndsAt, window.Channel);
            }
        }
    }
}
