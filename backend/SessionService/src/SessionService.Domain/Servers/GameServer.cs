using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace SessionService.Domain.Servers;

public sealed class GameServer
{
    public Guid Id { get; private set; }

    public string Address { get; private set; } = null!;

    public string Region { get; private set; } = null!;

    public IReadOnlyList<string> Modes { get; private set; } = [];

    public int Capacity { get; private set; }

    public GameServerStatus Status { get; private set; }

    public DateTime RegisteredAt { get; private set; }

    public DateTime LastHeartbeatAt { get; private set; }

    // EF Core
    private GameServer() { }

    public static Result<GameServer, Error> Register(
        Guid id,
        string address,
        string region,
        IEnumerable<string> modes,
        int capacity,
        DateTime now)
    {
        var server = new GameServer { Id = id, RegisteredAt = now };

        var updateResult = server.Update(address, region, modes, capacity, now);
        return updateResult.IsFailure ? updateResult.Error : server;
    }

    /// <summary>
    /// Re-registration after a restart keeps the id and refreshes the advertised settings.
    /// </summary>
    public UnitResult<Error> Update(
        string address,
        string region,
        IEnumerable<string> modes,
        int capacity,
        DateTime now)
    {
        string[] normalizedModes = [.. modes
            .Select(SessionNames.NormalizeMode)
            .Where(mode => mode.Length > 0)
            .Distinct(StringComparer.Ordinal)];

        string normalizedRegion = SessionNames.NormalizeRegion(region);

        if (string.IsNullOrWhiteSpace(address)
            || address.Length > SessionConstants.ADDRESS_MAX_LENGTH
            || normalizedRegion.Length is 0 or > SessionConstants.REGION_MAX_LENGTH
            || normalizedModes.Length is 0 or > SessionConstants.MAX_MODES
            || normalizedModes.Any(mode => mode.Length > SessionConstants.MODE_MAX_LENGTH)
            || capacity is <= 0 or > SessionConstants.MAX_CAPACITY)
        {
            return SessionErrors.ServerInvalid;
        }

        Address = address.Trim();
        Region = normalizedRegion;
        Modes = normalizedModes;
        Capacity = capacity;
        Status = GameServerStatus.Online;
        LastHeartbeatAt = now;

        return UnitResult.Success<Error>();
    }

    public bool Supports(string mode) => Modes.Contains(SessionNames.NormalizeMode(mode), StringComparer.Ordinal);

    public void Heartbeat(DateTime now)
    {
        Status = GameServerStatus.Online;
        LastHeartbeatAt = now;
    }

    public void MarkOffline() => Status = GameServerStatus.Offline;
}
