namespace SessionService.Contracts.Dtos;

/// <summary>
/// Public key game servers can use to check join tickets offline.
/// </summary>
public sealed record TicketKeyDto(string KeyId, string Algorithm, string PublicKeyPem);
