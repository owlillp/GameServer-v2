using CatalogService.Contracts.Dtos;
using CatalogService.Core.Abstractions;
using CatalogService.Domain.Compatibility;
using CatalogService.Domain.Items;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace CatalogService.Core.Features.Compatibility.Queries.GetCompatibleParts;

public sealed class GetCompatiblePartsHandler(IReadDbContext dbContext)
    : IQueryHandler<IReadOnlyList<CompatiblePartDto>, GetCompatiblePartsQuery>
{
    public async Task<Result<IReadOnlyList<CompatiblePartDto>, Error>> Handle(
        GetCompatiblePartsQuery query,
        CancellationToken cancellationToken = default)
    {
        Item? car = await dbContext.ItemsRead
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == query.CarItemId && item.Status == ItemStatus.Published,
                cancellationToken);

        if (car is null)
        {
            return CatalogErrors.ItemNotFound(query.CarItemId);
        }

        if (car.Kind != ItemKind.Car)
        {
            return GeneralErrors.ValueIsInvalid("carItemId");
        }

        string? slot = string.IsNullOrWhiteSpace(query.Slot) ? null : query.Slot.Trim().ToLowerInvariant();

        List<Item> parts = await dbContext.ItemsRead
            .AsNoTracking()
            .Where(item => item.Kind == ItemKind.CarPart && item.Status == ItemStatus.Published)
            .ToListAsync(cancellationToken);

        IEnumerable<Item> compatible = parts.Where(part => CompatibilityEvaluator.IsCompatible(car, part));

        if (slot is not null)
        {
            compatible = compatible.Where(part => string.Equals(part.Slot, slot, StringComparison.Ordinal));
        }

        IReadOnlyList<CompatiblePartDto> result = [.. compatible
            .OrderBy(part => part.DisplayName, StringComparer.Ordinal)
            .Select(part => new CompatiblePartDto(
                part.Id,
                part.Code,
                part.DisplayName,
                part.Slot,
                part.Rarity.ToString(),
                part.FitTags.Contains(CatalogConstants.UNIVERSAL_FIT_TAG, StringComparer.Ordinal)))];

        return Result.Success<IReadOnlyList<CompatiblePartDto>, Error>(result);
    }
}
