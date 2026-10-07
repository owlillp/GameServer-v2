using CatalogService.Contracts.Dtos;
using CatalogService.Contracts.Requests;
using CatalogService.Core.Abstractions;
using CatalogService.Core.Cache;
using CatalogService.Core.Features.Items.Models;
using CatalogService.Domain.Items;
using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;

namespace CatalogService.Core.Features.Items.Commands.CreateItem;

public sealed class CreateItemHandler(
    IValidator<CreateItemCommand> validator,
    IReadDbContext dbContext,
    IItemRepository itemRepository,
    ITransactionManager transactionManager,
    CatalogCacheInvalidator cacheInvalidator) : ICommandHandler<ItemDetailDto, CreateItemCommand>
{
    public async Task<Result<ItemDetailDto, Error>> Handle(
        CreateItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        CreateItemRequest request = command.Request;

        var kindResult = ItemKindParser.Parse(request.Kind);
        if (kindResult.IsFailure)
        {
            return kindResult.Error;
        }

        var rarityResult = ItemRarityParser.Parse(request.Rarity);
        if (rarityResult.IsFailure)
        {
            return rarityResult.Error;
        }

        var itemResult = Item.Create(
            request.Code,
            kindResult.Value,
            rarityResult.Value,
            request.DisplayName,
            request.Description,
            request.ModelCode,
            request.Slot,
            request.Tags,
            request.FitTags);

        if (itemResult.IsFailure)
        {
            return itemResult.Error;
        }

        Item item = itemResult.Value;

        bool codeExists = await dbContext.ItemsRead
            .AnyAsync(existing => existing.Code == item.Code, cancellationToken);

        if (codeExists)
        {
            return CatalogErrors.CodeNotUnique;
        }

        ApplyRelations(item, request);

        var addResult = await itemRepository.AddAsync(item, cancellationToken);
        if (addResult.IsFailure)
        {
            return addResult.Error;
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        await cacheInvalidator.InvalidateListsAsync(cancellationToken);

        return ItemMapper.ToDetailDto(item);
    }

    private static void ApplyRelations(Item item, CreateItemRequest request)
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
