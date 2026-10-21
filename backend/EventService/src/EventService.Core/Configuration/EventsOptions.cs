namespace EventService.Core.Configuration;

public sealed class EventsOptions
{
    public const string SECTION_NAME = "Events";

    public int ReconcileRetrySeconds { get; init; } = 15;

    public bool QuartzClustered { get; init; }

    public int QuartzMisfireThresholdSeconds { get; init; } = 300;
}
