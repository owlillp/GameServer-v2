using System.Text.Json;
using CatalogService.Contracts.Dtos;
using CatalogService.Core.Abstractions;
using CatalogService.Core.Cache;
using CatalogService.Core.Features.Items.Models;
using CatalogService.Core.Messaging.Publishers;
using CatalogService.Domain.Items;
using CatalogService.Domain.Versions;
using CSharpFunctionalExtensions;
using FluentValidation;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;

namespace CatalogService.Core.Features.Versions.Commands.PublishVersion;

public sealed class PublishVersionHandler(
    IValidator<PublishVersionCommand> validator,
    IItemRepository itemRepository,
    ICatalogVersionRepository versionRepository,
    ITransactionManager transactionManager,
    ICatalogEventPublisher eventPublisher,
    CatalogCacheInvalidator cacheInvalidator) : ICommandHandler<CatalogVersionDto, PublishVersionCommand>
{
    public async Task<Result<CatalogVersionDto, Error>> Handle(
        PublishVersionCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        var latestResult = await versionRepository.GetLatestNumberAsync(cancellationToken);
        if (latestResult.IsFailure)
        {
            return latestResult.Error;
        }

        long nextNumber = (latestResult.Value ?? 0) + 1;

        var itemsResult = await itemRepository.ListAsync(includeArchived: false, cancellationToken);
        if (itemsResult.IsFailure)
        {
            return itemsResult.Error;
        }

        var versionResult = CatalogVersion.Create(nextNumber, command.Request.Note, command.PublishedBy);
        if (versionResult.IsFailure)
        {
            return versionResult.Error;
        }

        CatalogVersion version = versionResult.Value;

        foreach (Item item in itemsResult.Value)
        {
            item.MarkPublished(nextNumber);
            version.AddItem(item, JsonSerializer.Serialize(CatalogItemSnapshot.From(item)));
        }

        var addResult = await versionRepository.AddAsync(version, cancellationToken);
        if (addResult.IsFailure)
        {
            return addResult.Error;
        }

        await eventPublisher.PublishVersionPublishedAsync(
            version.Id,
            version.Number,
            version.Items.Count,
            version.Note);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        await cacheInvalidator.InvalidateAllAsync(cancellationToken);

        return ItemMapper.ToDto(version);
    }
}
