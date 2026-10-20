using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace EventService.Domain.Events;

public sealed class EventReward
{
    public Guid Id { get; private set; }

    public Guid EventId { get; private set; }

    public string Code { get; private set; } = null!;

    public string DisplayName { get; private set; } = null!;

    public string? Description { get; private set; }

    public int PointsCost { get; private set; }

    public Guid CatalogItemId { get; private set; }

    public string ItemCode { get; private set; } = null!;

    public string ItemKind { get; private set; } = null!;

    public string? Slot { get; private set; }

    public int Quantity { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsRepeatable { get; private set; }

    // EF Core
    private EventReward() { }

    private EventReward(
        Guid eventId,
        string code,
        string displayName,
        string? description,
        int pointsCost,
        Guid catalogItemId,
        string itemCode,
        string itemKind,
        string? slot,
        int quantity,
        int sortOrder,
        bool isRepeatable)
    {
        Id = Guid.CreateVersion7();
        EventId = eventId;
        Code = code;
        DisplayName = displayName;
        Description = description;
        PointsCost = pointsCost;
        CatalogItemId = catalogItemId;
        ItemCode = itemCode;
        ItemKind = itemKind;
        Slot = slot;
        Quantity = quantity;
        SortOrder = sortOrder;
        IsRepeatable = isRepeatable;
    }

    internal static Result<EventReward, Error> Create(Guid eventId, EventRewardData data)
    {
        if (eventId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("eventId");
        }

        if (string.IsNullOrWhiteSpace(data.Code))
        {
            return GeneralErrors.ValueIsRequired("reward.code");
        }

        string code = data.Code.Trim().ToLowerInvariant();
        if (code.Length > EventConstants.CODE_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("reward.code", max: EventConstants.CODE_MAX_LENGTH);
        }

        if (string.IsNullOrWhiteSpace(data.DisplayName))
        {
            return GeneralErrors.ValueIsRequired("reward.displayName");
        }

        string displayName = data.DisplayName.Trim();
        if (displayName.Length > EventConstants.DISPLAY_NAME_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("reward.displayName", max: EventConstants.DISPLAY_NAME_MAX_LENGTH);
        }

        string? description = string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim();
        if (description is not null && description.Length > EventConstants.DESCRIPTION_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("reward.description", max: EventConstants.DESCRIPTION_MAX_LENGTH);
        }

        if (data.PointsCost <= 0)
        {
            return EventErrors.RewardCostInvalid;
        }

        if (data.CatalogItemId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("reward.catalogItemId");
        }

        if (string.IsNullOrWhiteSpace(data.ItemCode) || string.IsNullOrWhiteSpace(data.ItemKind))
        {
            return GeneralErrors.ValueIsRequired("reward.item");
        }

        string itemCode = data.ItemCode.Trim().ToLowerInvariant();
        if (itemCode.Length > EventConstants.ITEM_CODE_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("reward.itemCode", max: EventConstants.ITEM_CODE_MAX_LENGTH);
        }

        string itemKind = data.ItemKind.Trim();
        if (itemKind.Length > EventConstants.ITEM_KIND_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("reward.itemKind", max: EventConstants.ITEM_KIND_MAX_LENGTH);
        }

        string? slot = string.IsNullOrWhiteSpace(data.Slot) ? null : data.Slot.Trim().ToLowerInvariant();
        if (slot is not null && slot.Length > EventConstants.SLOT_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("reward.slot", max: EventConstants.SLOT_MAX_LENGTH);
        }

        if (data.Quantity <= 0)
        {
            return EventErrors.QuantityInvalid;
        }

        return new EventReward(
            eventId,
            code,
            displayName,
            description,
            data.PointsCost,
            data.CatalogItemId,
            itemCode,
            itemKind,
            slot,
            data.Quantity,
            data.SortOrder,
            data.IsRepeatable);
    }
}
