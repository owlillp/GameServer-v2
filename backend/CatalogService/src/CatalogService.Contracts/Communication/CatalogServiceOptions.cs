namespace CatalogService.Contracts.Communication;

public sealed class CatalogServiceOptions
{
    public const string SECTION_NAME = "CatalogService";

    public string Url { get; init; } = string.Empty;

    public int TimeoutSeconds { get; init; } = 5;
}
