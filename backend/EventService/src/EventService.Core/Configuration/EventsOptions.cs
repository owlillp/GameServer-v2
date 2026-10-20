namespace EventService.Core.Configuration;

public sealed class EventsOptions
{
    public const string SECTION_NAME = "Events";

    public int PhaseSweepIntervalSeconds { get; init; } = 30;
}
