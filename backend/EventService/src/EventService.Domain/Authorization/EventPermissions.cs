namespace EventService.Domain.Authorization;

public static class EventPermissions
{
    public static class Events
    {
        public const string VIEW = "events.view";
        public const string MANAGE = "events.manage";
    }

    public static IReadOnlyList<string> All { get; } =
    [
        Events.VIEW,
        Events.MANAGE,
    ];
}
