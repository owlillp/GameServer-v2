using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using SessionService.Contracts.Dtos;
using SessionService.Core.Abstractions;
using SessionService.Core.Features.Sessions.Models;
using SessionService.Domain;
using SessionService.Domain.Servers;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace SessionService.Core.Features.Admin.Queries.ListServers;

public sealed class ListServersHandler(IReadDbContext dbContext)
    : IQueryHandler<IReadOnlyList<GameServerDto>, ListServersQuery>
{
    public async Task<Result<IReadOnlyList<GameServerDto>, Error>> Handle(
        ListServersQuery query,
        CancellationToken cancellationToken = default)
    {
        IQueryable<GameServer> servers = dbContext.ServersRead;

        if (!string.IsNullOrWhiteSpace(query.Region))
        {
            string region = SessionNames.NormalizeRegion(query.Region);
            servers = servers.Where(server => server.Region == region);
        }

        List<GameServer> result = await servers
            .OrderByDescending(server => server.LastHeartbeatAt)
            .Take(500)
            .ToListAsync(cancellationToken);

        return result.Select(server => server.ToDto()).ToList();
    }
}
