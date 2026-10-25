namespace WalletService.Web.Grpc;

public sealed class GrpcAuthOptions
{
    public const string SECTION_NAME = "Grpc";

    /// <summary>
    /// Single allowed client (kept for backward compatibility with existing configs).
    /// </summary>
    public string AllowedClientId { get; init; } = string.Empty;

    public IReadOnlyList<string> AllowedClientIds { get; init; } = [];

    public IEnumerable<string> AllClientIds =>
        AllowedClientIds
            .Append(AllowedClientId)
            .Where(clientId => !string.IsNullOrWhiteSpace(clientId));
}
