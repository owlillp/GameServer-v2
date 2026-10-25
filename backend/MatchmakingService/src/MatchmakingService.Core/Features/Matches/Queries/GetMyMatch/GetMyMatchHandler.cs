using CSharpFunctionalExtensions;
using MatchmakingService.Contracts.Dtos;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Features.Matchmaking.Models;
using MatchmakingService.Domain.Matches;
using Microsoft.Extensions.Logging;
using SessionService.Contracts.Communication;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Features.Matches.Queries.GetMyMatch;

/// <summary>
/// The player's active match. Once a game server is allocated, the response also carries a
/// fresh join ticket from SessionService; if SessionService is unavailable the match is
/// returned without it and the client asks again.
/// </summary>
public sealed class GetMyMatchHandler(
    IMatchRepository matchRepository,
    ISessionTicketClient sessionTicketClient,
    ILogger<GetMyMatchHandler> logger) : IQueryHandler<MatchDetailsDto?, GetMyMatchQuery>
{
    public async Task<Result<MatchDetailsDto?, Error>> Handle(
        GetMyMatchQuery query,
        CancellationToken cancellationToken = default)
    {
        Match? match = await matchRepository.GetActiveByAccountAsync(query.AccountId, cancellationToken);
        if (match is null)
        {
            return Result.Success<MatchDetailsDto?, Error>(null);
        }

        MatchDetailsDto details = match.ToDetailsDto(query.AccountId);
        if (!match.IsAllocated)
        {
            return details;
        }

        var ticketResult = await sessionTicketClient.IssueMatchTicketAsync(match.Id, query.AccountId, cancellationToken);
        if (ticketResult.IsFailure)
        {
            logger.LogWarning(
                "No join ticket for match {MatchId}: {Error}",
                match.Id,
                ticketResult.Error.GetMessage());

            return details;
        }

        MatchTicket ticket = ticketResult.Value;

        return details with
        {
            Connection = new MatchConnectionDto(ticket.SessionId, ticket.ServerAddress, ticket.Ticket, ticket.ExpiresAt),
        };
    }
}
