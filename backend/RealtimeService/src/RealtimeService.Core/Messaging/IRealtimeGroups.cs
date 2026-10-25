namespace RealtimeService.Core.Messaging;

public interface IRealtimeGroups
{
    Task AddAsync(string connectionId, string group, CancellationToken cancellationToken);

    Task RemoveAsync(string connectionId, string group, CancellationToken cancellationToken);
}
