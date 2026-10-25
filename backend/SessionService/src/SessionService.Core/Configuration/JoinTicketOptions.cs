using System.ComponentModel.DataAnnotations;

namespace SessionService.Core.Configuration;

public sealed class JoinTicketOptions
{
    public const string SECTION_NAME = "JoinTickets";

    [Required]
    public string Issuer { get; init; } = "session-service";

    [Required]
    public string Audience { get; init; } = "game-server";

    [Range(5, 600)]
    public int LifetimeSeconds { get; init; } = 60;

    /// <summary>
    /// RSA private key in PEM. In production it comes from Lockbox through the environment.
    /// </summary>
    public string SigningKeyPem { get; init; } = string.Empty;

    [Required]
    public string KeyId { get; init; } = "session-ticket-1";

    /// <summary>
    /// Dev only: without a configured key, generate one at startup. Tickets then do not survive a
    /// restart and do not work across replicas.
    /// </summary>
    public bool AllowEphemeralKey { get; init; }
}
