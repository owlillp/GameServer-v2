using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;
using SessionService.Contracts.Dtos;
using SessionService.Core.Abstractions;
using SessionService.Core.Configuration;
using SessionService.Core.Services;
using SessionService.Domain;
using SessionService.Domain.Sessions;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace SessionService.Core.Features.Sessions.Commands.JoinSession;

public sealed class JoinSessionHandler(
    ISessionRepository sessionRepository,
    SessionPlacement placement,
    IOptionsMonitor<SessionOptions> options,
    TimeProvider timeProvider) : ICommandHandler<SessionTicketDto, JoinSessionCommand>
{
    public async Task<Result<SessionTicketDto, Error>> Handle(
        JoinSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        string region = SessionNames.NormalizeRegion(command.Region);
        if (region.Length is 0 or > SessionConstants.REGION_MAX_LENGTH)
        {
            return SessionErrors.RegionInvalid;
        }

        string mode = SessionNames.NormalizeMode(
            string.IsNullOrWhiteSpace(command.Mode) ? options.CurrentValue.DefaultFreeRoamMode : command.Mode);

        if (mode.Length > SessionConstants.MODE_MAX_LENGTH)
        {
            return SessionErrors.ModeInvalid;
        }

        // Reconnect: a player who is still inside an open-world session goes back to it.
        Session? current = await sessionRepository.GetCurrentByPlayerAsync(command.AccountId, cancellationToken);
        if (current is { Kind: SessionKind.FreeRoam }
            && string.Equals(current.Region, region, StringComparison.Ordinal)
            && string.Equals(current.Mode, mode, StringComparison.Ordinal))
        {
            return await placement.IssueAsync(
                command.AccountId,
                current,
                timeProvider.GetUtcNow().UtcDateTime,
                cancellationToken);
        }

        return await placement.JoinFreeRoamAsync(command.AccountId, region, mode, cancellationToken);
    }
}
