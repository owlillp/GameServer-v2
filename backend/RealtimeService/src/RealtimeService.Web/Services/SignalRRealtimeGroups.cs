using Microsoft.AspNetCore.SignalR;
using RealtimeService.Core.Messaging;
using RealtimeService.Web.Hubs;

namespace RealtimeService.Web.Services;

/// <summary>
/// Group changes go through the Redis backplane, so they reach connections on other nodes.
/// </summary>
public sealed class SignalRRealtimeGroups(IHubContext<RealtimeHub> hubContext) : IRealtimeGroups
{
    public Task AddAsync(string connectionId, string group, CancellationToken cancellationToken) =>
        hubContext.Groups.AddToGroupAsync(connectionId, group, cancellationToken);

    public Task RemoveAsync(string connectionId, string group, CancellationToken cancellationToken) =>
        hubContext.Groups.RemoveFromGroupAsync(connectionId, group, cancellationToken);
}
