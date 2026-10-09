using CSharpFunctionalExtensions;
using CatalogService.Domain.Items;
using Shared.SharedKernel.Errors;

namespace CatalogService.Domain.Compatibility;

public static class CompatibilityEvaluator
{
    public static bool IsCompatible(Item car, Item part)
    {
        if (car.Kind != ItemKind.CAR || part.Kind != ItemKind.CAR_PART)
        {
            return false;
        }

        if (part.FitTags.Length == 0)
        {
            return false;
        }

        if (part.FitTags.Contains(CatalogConstants.UNIVERSAL_FIT_TAG, StringComparer.Ordinal))
        {
            return true;
        }

        var capabilities = car.Capabilities();
        return part.FitTags.Any(tag => capabilities.Contains(tag, StringComparer.Ordinal));
    }

    public static UnitResult<Error> Ensure(Item car, Item part) =>
        IsCompatible(car, part)
            ? UnitResult.Success<Error>()
            : CatalogErrors.NotCompatible;
}
