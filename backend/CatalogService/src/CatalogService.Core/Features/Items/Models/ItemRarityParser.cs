using CatalogService.Domain.Items;
using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace CatalogService.Core.Features.Items.Models;

internal static class ItemRarityParser
{
    public static Result<ItemRarity, Error> Parse(string value)
    {
        if (Enum.TryParse(value, ignoreCase: true, out ItemRarity rarity) && Enum.IsDefined(rarity))
        {
            return rarity;
        }

        return GeneralErrors.ValueIsInvalid("rarity");
    }
}
