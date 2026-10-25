using CSharpFunctionalExtensions;
using SessionService.Contracts.Dtos;
using SessionService.Core.Abstractions;
using SessionService.Core.Features.Sessions.Models;
using SessionService.Domain;
using SessionService.Domain.Sessions;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace SessionService.Core.Features.Sessions.Queries.GetMySession;

public sealed class GetMySessionHandler(ISessionRepository sessionRepository)
    : IQueryHandler<SessionDto, GetMySessionQuery>
{
    public async Task<Result<SessionDto, Error>> Handle(
        GetMySessionQuery query,
        CancellationToken cancellationToken = default)
    {
        Session? session = await sessionRepository.GetCurrentByPlayerAsync(query.AccountId, cancellationToken);

        return session is null ? SessionErrors.SessionNotFound : session.ToDto();
    }
}
