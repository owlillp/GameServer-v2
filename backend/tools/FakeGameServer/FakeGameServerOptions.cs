namespace FakeGameServer;

public sealed class FakeGameServerOptions
{
    public const string SECTION_NAME = "FakeGameServer";

    public string SessionGrpcAddress { get; init; } = "http://localhost:5290";

    /// <summary>
    /// Fixed id keeps the same server row across restarts; empty lets SessionService assign one.
    /// </summary>
    public string ServerId { get; init; } = string.Empty;

    /// <summary>
    /// Address players are told to connect to.
    /// </summary>
    public string PublicAddress { get; init; } = "localhost:5291";

    public string Region { get; init; } = "eu-west";

    public IReadOnlyList<string> Modes { get; init; } = ["FREE-ROAM", "RANKED"];

    public int Capacity { get; init; } = 64;

    /// <summary>
    /// Sessions without players are dropped from heartbeats after this long, so SessionService closes them.
    /// </summary>
    public int EmptySessionSeconds { get; init; } = 120;
}
