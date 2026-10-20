namespace Shared.Communication;

public sealed class ServiceAuthOptions
{
    public const string SECTION_NAME = "ServiceAuth";

    public string Authority { get; init; } = string.Empty;

    public string ClientId { get; init; } = string.Empty;

    public string ClientSecret { get; init; } = string.Empty;

    public IReadOnlyList<string> Scopes { get; init; } = [];

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Authority)
        && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret);
}
