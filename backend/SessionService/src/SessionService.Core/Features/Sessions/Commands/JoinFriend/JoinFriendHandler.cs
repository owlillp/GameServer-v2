using CSharpFunctionalExtensions;
using SessionService.Contracts.Dtos;
using SessionService.Core.Abstractions;
using SessionService.Core.Services;
using SessionService.Domain;
using SessionService.Domain.Sessions;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace SessionService.Core.Features.Sessions.Commands.JoinFriend;

/// <summary>
/// Joins the open-world session a friend is in. The block-list check arrives with SocialService.
/// </summary>
public sealed class JoinFriendHandler(
    ISessionRepository sessionRepository,
    ISessionRegistry registry,
    SessionPlacement placement,
    TimeProvider timeProvider) : ICommandHandler<SessionTicketDto, JoinFriendCommand>
{
    public async Task<Result<SessionTicketDto, Error>> Handle(
        JoinFriendCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.AccountId == command.FriendAccountId)
        {
            return SessionErrors.FriendNotInSession;
        }

        Session? session = await sessionRepository.GetCurrentByPlayerAsync(command.FriendAccountId, cancellationToken);
        if (session is null || session.Kind != SessionKind.FreeRoam)
        {
            return SessionErrors.FriendNotInSession;
        }

        if (!session.IsPresent(command.AccountId) && !session.HasFreeSlot)
        {
            return SessionErrors.SessionFull;
        }

        IReadOnlySet<Guid> alive = await registry.GetAliveAsync([session.Id], cancellationToken);
        if (!alive.Contains(session.Id))
        {
            return SessionErrors.FriendNotInSession;
        }

        return await placement.IssueAsync(
            command.AccountId,
            session,
            timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);
    }
}
