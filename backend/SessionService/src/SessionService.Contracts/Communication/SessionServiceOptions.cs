namespace SessionService.Contracts.Communication;

public sealed class SessionServiceOptions
{
    public const string SECTION_NAME = "SessionService";

    public string GrpcAddress { get; init; } = string.Empty;

    public int DeadlineSeconds { get; init; } = 3;

    public int MaxAttempts { get; init; } = 2;
}
