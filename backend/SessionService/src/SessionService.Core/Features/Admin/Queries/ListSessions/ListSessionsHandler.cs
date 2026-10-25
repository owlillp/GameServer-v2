using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using SessionService.Contracts.Dtos;
using SessionService.Core.Abstractions;
using SessionService.Core.Features.Sessions.Models;
using SessionService.Domain.Sessions;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace SessionService.Core.Features.Admin.Queries.ListSessions;

public sealed class ListSessionsHandler(IReadDbContext dbContext)
    : IQueryHandler<IReadOnlyList<SessionDto>, ListSessionsQuery>
{
    private const int MAX_LIMIT = 500;

    public async Task<Result<IReadOnlyList<SessionDto>, Error>> Handle(
        ListSessionsQuery query,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Session> sessions = dbContext.SessionsRead;

        if (query.OpenOnly)
        {
            sessions = sessions.Where(session =>
                session.Status == SessionStatus.Allocating || session.Status == SessionStatus.Active);
        }

        List<Session> result = await sessions
            .OrderByDescending(session => session.CreatedAt)
            .Take(Math.Clamp(query.Limit, 1, MAX_LIMIT))
            .ToListAsync(cancellationToken);

        return result.Select(session => session.ToDto()).ToList();
    }
}
