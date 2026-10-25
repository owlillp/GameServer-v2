using System.ComponentModel.DataAnnotations;

namespace SessionService.Web.Grpc;

public sealed class GameServerAuthOptions
{
    public const string SECTION_NAME = "GameServerAuth";

    [MinLength(1)]
    public IReadOnlyList<string> AllowedClientIds { get; init; } = [];

    [Required]
    public string RequiredScope { get; init; } = "session.write";
}
