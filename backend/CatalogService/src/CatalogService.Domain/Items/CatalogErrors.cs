using Shared.SharedKernel.Errors;

namespace CatalogService.Domain.Items;

public static class CatalogErrors
{
    public static Error ItemNotFound(Guid itemId) =>
        GeneralErrors.NotFound(itemId, entityName: "item");

    public static Error CodeNotUnique { get; } =
        Error.Conflict("catalog.item.code.duplicate", "Item code already exists", "code");

    public static Error ItemNotEditable { get; } =
        Error.Validation("catalog.item.not_editable", "Published item cannot be edited; un-publish it first", "itemId");

    public static Error ItemAlreadyArchived { get; } =
        Error.Validation("catalog.item.already_archived", "Item is already archived", "itemId");

    public static Error FitTagsRequired { get; } =
        Error.Validation("catalog.item.fit_tags.required", "Car part must declare at least one fit tag", "fitTags");

    public static Error ModelCodeRequired { get; } =
        Error.Validation("catalog.item.model_code.required", "Car must declare a model code", "modelCode");

    public static Error VersionNotFound { get; } = GeneralErrors.NotFound();

    public static Error NoPublishedVersion { get; } = GeneralErrors.NotFound();

    public static Error NotCompatible { get; } =
        Error.Validation("catalog.compatibility.mismatch", "Part is not compatible with the selected car", "partItemId");
}
