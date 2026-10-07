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

namespace CatalogService.Core.Features.Versions.Commands.RollbackVersion;

public sealed class RollbackVersionHandler(
    IValidator<RollbackVersionCommand> validator,
    IItemRepository itemRepository,
    ICatalogVersionRepository versionRepository,
    ITransactionManager transactionManager,
    ICatalogEventPublisher eventPublisher,
    CatalogCacheInvalidator cacheInvalidator) : ICommandHandler<CatalogVersionDto, RollbackVersionCommand>
{
    public async Task<Result<CatalogVersionDto, Error>> Handle(
        RollbackVersionCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        var targetResult = await versionRepository.GetByNumberAsync(command.Request.TargetNumber, cancellationToken);
        if (targetResult.IsFailure)
        {
            return targetResult.Error;
        }

        var latestResult = await versionRepository.GetLatestNumberAsync(cancellationToken);
        if (latestResult.IsFailure)
        {
            return latestResult.Error;
        }

        long nextNumber = (latestResult.Value ?? 0) + 1;

        var existingResult = await itemRepository.ListAsync(includeArchived: true, cancellationToken);
        if (existingResult.IsFailure)
        {
            return existingResult.Error;
        }

        var existingById = existingResult.Value.ToDictionary(item => item.Id);
        var restored = new List<Item>(targetResult.Value.Items.Count);
        var added = new List<Item>();

        foreach (CatalogVersionItem versionItem in targetResult.Value.Items)
        {
            CatalogItemSnapshot? snapshot = JsonSerializer.Deserialize<CatalogItemSnapshot>(versionItem.Snapshot);
            if (snapshot is null)
            {
                return GeneralErrors.Failure("Process rollback version");
            }

            if (existingById.TryGetValue(snapshot.Id, out Item? existing))
            {
                existing.ApplySnapshot(snapshot, nextNumber);
                restored.Add(existing);
            }
            else
            {
                Item created = snapshot.ToItem(nextNumber);
                restored.Add(created);
                added.Add(created);
            }
        }

        var restoredIds = restored.Select(item => item.Id).ToHashSet();
        foreach (Item item in existingResult.Value)
        {
            if (!restoredIds.Contains(item.Id) && item.Status != ItemStatus.Archived)
            {
                item.Archive();
            }
        }

        var versionResult = CatalogVersion.Create(nextNumber, command.Request.Note, command.PublishedBy);
        if (versionResult.IsFailure)
        {
            return versionResult.Error;
        }

        CatalogVersion version = versionResult.Value;
        foreach (Item item in restored)
        {
            version.AddItem(item, JsonSerializer.Serialize(CatalogItemSnapshot.From(item)));
        }

        if (added.Count > 0)
        {
            var addItemsResult = await itemRepository.AddRangeAsync(added, cancellationToken);
            if (addItemsResult.IsFailure)
            {
                return addItemsResult.Error;
            }
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
