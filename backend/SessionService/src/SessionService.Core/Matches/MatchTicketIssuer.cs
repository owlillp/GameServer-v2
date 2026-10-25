using CSharpFunctionalExtensions;
using SessionService.Contracts.Dtos;
using SessionService.Core.Abstractions;
using SessionService.Core.Services;
using SessionService.Domain;
using SessionService.Domain.Sessions;
using Shared.SharedKernel.Errors;

namespace SessionService.Core.Matches;

/// <summary>
/// Join tickets for queued matches. Matchmaking decides who is a participant and asks
/// on the player's behalf; the session only has to be allocated and open.
/// </summary>
public sealed class MatchTicketIssuer(
    ISessionRepository sessionRepository,
    SessionPlacement placement,
    TimeProvider timeProvider)
{
    public async Task<Result<SessionTicketDto, Error>> IssueAsync(
        Guid matchId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        Session? session = await sessionRepository.GetByMatchAsync(matchId, cancellationToken);
        if (session is null || !session.IsOpen)
        {
            return SessionErrors.MatchSessionNotReady;
        }

        return await placement.IssueAsync(accountId, session, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
    }
}
