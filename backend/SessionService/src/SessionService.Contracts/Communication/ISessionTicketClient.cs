using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace SessionService.Contracts.Communication;

public sealed record MatchTicket(Guid SessionId, string ServerAddress, string Ticket, DateTime ExpiresAt);

public interface ISessionTicketClient
{
    /// <summary>
    /// Join ticket for a participant of a match whose session is allocated.
    /// </summary>
    Task<Result<MatchTicket, Error>> IssueMatchTicketAsync(
        Guid matchId,
        Guid accountId,
        CancellationToken cancellationToken);
}
