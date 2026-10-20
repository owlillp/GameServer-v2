namespace StoreService.Core.Configuration;

public sealed class StoreOptions
{
    public const string SECTION_NAME = "Store";

    public int OrderTimeoutMinutes { get; init; } = 15;
}
