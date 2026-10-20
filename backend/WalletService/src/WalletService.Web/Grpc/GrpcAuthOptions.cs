namespace WalletService.Web.Grpc;

public sealed class GrpcAuthOptions
{
    public const string SECTION_NAME = "Grpc";

    public string AllowedClientId { get; init; } = string.Empty;
}
