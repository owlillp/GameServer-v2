using System.ComponentModel.DataAnnotations;

namespace SessionService.Web.Grpc;

/// <summary>
/// Backend services (matchmaking) allowed to call the <c>MatchSessions</c> gRPC API.
/// </summary>
public sealed class ServiceClientAuthOptions
{
    public const string SECTION_NAME = "ServiceClientAuth";

    [MinLength(1)]
    public IReadOnlyList<string> AllowedClientIds { get; init; } = [];

    [Required]
    public string RequiredScope { get; init; } = "session";
}
