using ClanService.Contracts.Dtos;
using ClanService.Core.Abstractions;
using ClanService.Core.Features.Clans.Models;
using ClanService.Core.Services;
using ClanService.Domain;
using ClanService.Domain.Clans;
using ClanService.Domain.Members;
using ClanService.Domain.Requests;
using CSharpFunctionalExtensions;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace ClanService.Core.Features.Clans.Commands;

public sealed record AcceptRequestCommand(Guid AccountId, Guid RequestId) : ICommand;

public sealed record DeclineRequestCommand(Guid AccountId, Guid RequestId) : ICommand;

/// <summary>
/// An invite is accepted by the invited player, a join request by an officer or the leader.
/// The member is added in the same transaction; the slot is taken by an atomic update.
/// </summary>
public sealed class AcceptRequestHandler(
    IClanRepository clanRepository,
    IClanRequestRepository requestRepository,
    ClanMembership membership,
    ITransactionManager transactionManager) : ICommandHandler<ClanMemberDto, AcceptRequestCommand>
{
    public async Task<Result<ClanMemberDto, Error>> Handle(AcceptRequestCommand command, CancellationToken cancellationToken = default)
    {
        ClanJoinRequest? request = await requestRepository.GetAsync(command.RequestId, cancellationToken);
        if (request is null)
        {
            return ClanErrors.RequestNotFound;
        }

        var allowed = await RequestAccess.EnsureDeciderAsync(request, command.AccountId, membership, cancellationToken);
        if (allowed.IsFailure)
        {
            return allowed.Error;
        }

        Clan? clan = await clanRepository.GetAsync(request.ClanId, cancellationToken);
        if (clan is null)
        {
            return ClanErrors.ClanNotFound;
        }

        var beginResult = await transactionManager.BeginTransactionAsync(cancellationToken);
        if (beginResult.IsFailure)
        {
            return beginResult.Error;
        }

        var acceptResult = request.Accept(command.AccountId, membership.Now);
        if (acceptResult.IsFailure)
        {
            return acceptResult.Error;
        }

        var memberResult = await membership.AddMemberAsync(clan, request.AccountId, cancellationToken);
        if (memberResult.IsFailure)
        {
            return memberResult.Error;
        }

        var commitResult = await transactionManager.CommitTransactionAsync(cancellationToken);
        return commitResult.IsFailure ? ClanSaveErrors.Map(commitResult.Error) : memberResult.Value.ToDto();
    }
}

public sealed class DeclineRequestHandler(
    IClanRequestRepository requestRepository,
    ClanMembership membership,
    ITransactionManager transactionManager) : ICommandHandler<bool, DeclineRequestCommand>
{
    public async Task<Result<bool, Error>> Handle(DeclineRequestCommand command, CancellationToken cancellationToken = default)
    {
        ClanJoinRequest? request = await requestRepository.GetAsync(command.RequestId, cancellationToken);
        if (request is null)
        {
            return ClanErrors.RequestNotFound;
        }

        UnitResult<Error> result;

        // The player withdraws their own request.
        if (!request.IsDecidedByPlayer && request.AccountId == command.AccountId)
        {
            result = request.Cancel(command.AccountId, membership.Now);
        }
        else
        {
            var allowed = await RequestAccess.EnsureDeciderAsync(request, command.AccountId, membership, cancellationToken);
            if (allowed.IsFailure)
            {
                return allowed.Error;
            }

            result = request.Decline(command.AccountId, membership.Now);
        }

        if (result.IsFailure)
        {
            return result.Error;
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        return true;
    }
}

internal static class RequestAccess
{
    public static async Task<UnitResult<Error>> EnsureDeciderAsync(
        ClanJoinRequest request,
        Guid accountId,
        ClanMembership membership,
        CancellationToken cancellationToken)
    {
        if (request.IsDecidedByPlayer)
        {
            return request.AccountId == accountId ? UnitResult.Success<Error>() : ClanErrors.RequestNotFound;
        }

        var actorResult = await membership.GetActorAsync(request.ClanId, accountId, cancellationToken);
        if (actorResult.IsFailure)
        {
            return ClanErrors.Forbidden;
        }

        return ClanPolicy.CanManageRequests(actorResult.Value.Role) ? UnitResult.Success<Error>() : ClanErrors.Forbidden;
    }
}
