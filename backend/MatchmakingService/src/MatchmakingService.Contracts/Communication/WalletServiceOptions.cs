namespace MatchmakingService.Contracts.Communication;

public sealed class WalletServiceOptions
{
    public const string SECTION_NAME = "WalletService";

    public string Url { get; init; } = string.Empty;

    public int TimeoutSeconds { get; init; } = 5;
}
