namespace SessionService.Domain;

/// <summary>
/// Regions are lower case ("eu-west"), modes upper case ("RANKED"), as in matchmaking.
/// </summary>
public static class SessionNames
{
    public static string NormalizeRegion(string? region) => (region ?? string.Empty).Trim().ToLowerInvariant();

    public static string NormalizeMode(string? mode) => (mode ?? string.Empty).Trim().ToUpperInvariant();
}
