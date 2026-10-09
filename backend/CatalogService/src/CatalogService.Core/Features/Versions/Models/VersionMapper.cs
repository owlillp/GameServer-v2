using System.Text.Json;
using CatalogService.Contracts.Dtos;
using CatalogService.Domain.Versions;

namespace CatalogService.Core.Features.Versions.Models;

internal static class VersionMapper
{
    public static CatalogVersionDto ToDto(CatalogVersion version, bool isCurrent) =>
        new(
            version.Id,
            version.Number,
            version.Note,
            version.PublishedBy,
            version.PublishedAt,
            version.Items.Count,
            StatusOf(isCurrent));

    public static CatalogVersionDetailDto ToDetailDto(CatalogVersion version, bool isCurrent)
    {
        List<CatalogVersionItemDto> items = [.. version.Items
            .Select(versionItem => JsonSerializer.Deserialize<CatalogItemSnapshot>(versionItem.Snapshot))
            .Where(snapshot => snapshot is not null)
            .Select(snapshot => ToItemDto(snapshot!))];

        return new(
            version.Id,
            version.Number,
            version.Note,
            version.PublishedBy,
            version.PublishedAt,
            items.Count,
            StatusOf(isCurrent),
            items);
    }

    private static string StatusOf(bool isCurrent) => isCurrent ? "CURRENT" : "SUPERSEDED";

    private static CatalogVersionItemDto ToItemDto(CatalogItemSnapshot snapshot) =>
        new(
            snapshot.Id,
            snapshot.Code,
            snapshot.Kind,
            snapshot.Rarity,
            snapshot.DisplayName,
            snapshot.ModelCode,
            snapshot.Slot,
            [.. snapshot.Tags],
            [.. snapshot.FitTags]);
}
