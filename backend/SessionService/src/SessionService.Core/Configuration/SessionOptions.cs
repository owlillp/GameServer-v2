using System.ComponentModel.DataAnnotations;

namespace SessionService.Core.Configuration;

public sealed class SessionOptions
{
    public const string SECTION_NAME = "Sessions";

    /// <summary>
    /// Interval the game server is told to send heartbeats at.
    /// </summary>
    [Range(1, 300)]
    public int HeartbeatIntervalSeconds { get; init; } = 5;

    /// <summary>
    /// A server whose heartbeat is older than this is not used for new sessions.
    /// </summary>
    [Range(1, 3600)]
    public int ServerTtlSeconds { get; init; } = 20;

    /// <summary>
    /// Live key lifetime of a session the server reports in its heartbeat.
    /// </summary>
    [Range(1, 3600)]
    public int SessionTtlSeconds { get; init; } = 30;

    /// <summary>
    /// Live key lifetime of a just created session: covers the join ticket and the first connect.
    /// </summary>
    [Range(1, 3600)]
    public int NewSessionTtlSeconds { get; init; } = 120;

    [Range(1, 1000)]
    public int FreeRoamMaxPlayers { get; init; } = 32;

    [Required]
    [MaxLength(32)]
    public string DefaultFreeRoamMode { get; init; } = "FREE-ROAM";

    [Range(1, 1000)]
    public int JoinCandidates { get; init; } = 20;

    [Range(1, 3600)]
    public int LivenessSweepIntervalSeconds { get; init; } = 10;

    [Range(1, 10000)]
    public int LivenessBatchSize { get; init; } = 100;

    /// <summary>
    /// The server row in PostgreSQL is updated at most this often; the Redis key on every heartbeat.
    /// </summary>
    [Range(1, 3600)]
    public int HeartbeatPersistSeconds { get; init; } = 30;
}
