namespace WalletService.Contracts.Communication;

public sealed class WalletServiceOptions
{
    public const string SECTION_NAME = "WalletService";

    public string GrpcAddress { get; init; } = string.Empty;

    public int DeadlineSeconds { get; init; } = 3;

    public int MaxAttempts { get; init; } = 2;
}
