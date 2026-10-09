using CatalogService.Domain.Items;
using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace CatalogService.Core.Features.Items.Models;

internal static class ItemKindParser
{
    public static Result<ItemKind, Error> Parse(string value)
    {
        if (Enum.TryParse(value, ignoreCase: true, out ItemKind kind) && Enum.IsDefined(kind))
        {
            return kind;
        }

        return GeneralErrors.ValueIsInvalid("kind");
    }
}
