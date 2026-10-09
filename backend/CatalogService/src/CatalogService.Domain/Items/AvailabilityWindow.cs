using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace CatalogService.Domain.Items;

public sealed class AvailabilityWindow
{
    public Guid Id { get; private set; }

    public Guid ItemId { get; private set; }

    public DateTime? StartsAt { get; private set; }

    public DateTime? EndsAt { get; private set; }

    public string? Channel { get; private set; }

    // EF Core
    private AvailabilityWindow() { }

    private AvailabilityWindow(Guid itemId, DateTime? startsAt, DateTime? endsAt, string? channel)
    {
        Id = Guid.CreateVersion7();
        ItemId = itemId;
        StartsAt = startsAt;
        EndsAt = endsAt;
        Channel = channel;
    }

    internal static Result<AvailabilityWindow, Error> Create(
        Guid itemId,
        DateTime? startsAt,
        DateTime? endsAt,
        string? channel)
    {
        if (startsAt is not null && endsAt is not null && endsAt <= startsAt)
        {
            return GeneralErrors.ValueIsInvalid("availability.endsAt");
        }

        return new AvailabilityWindow(itemId, startsAt, endsAt, string.IsNullOrWhiteSpace(channel) ? null : channel.Trim());
    }

    internal bool IsActiveAt(DateTime moment) =>
        (StartsAt is null || StartsAt <= moment) && (EndsAt is null || EndsAt > moment);
}
