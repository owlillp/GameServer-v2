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

public sealed record InvitePlayerCommand(Guid AccountId, Guid ClanId, Guid InvitedAccountId) : ICommand;

public sealed class InvitePlayerHandler(
    IClanRepository clanRepository,
    IClanMemberRepository memberRepository,
    IClanRequestRepository requestRepository,
    ClanMembership membership,
    ITransactionManager transactionManager) : ICommandHandler<ClanJoinRequestDto, InvitePlayerCommand>
{
    public async Task<Result<ClanJoinRequestDto, Error>> Handle(InvitePlayerCommand command, CancellationToken cancellationToken = default)
    {
        var actorResult = await membership.GetActorAsync(command.ClanId, command.AccountId, cancellationToken);
        if (actorResult.IsFailure)
        {
            return actorResult.Error;
        }

        if (!ClanPolicy.CanManageRequests(actorResult.Value.Role))
        {
            return ClanErrors.Forbidden;
        }

        Clan? clan = await clanRepository.GetAsync(command.ClanId, cancellationToken);
        if (clan is null || !clan.IsActive)
        {
            return ClanErrors.ClanNotFound;
        }

        if (await memberRepository.GetAsync(command.InvitedAccountId, cancellationToken) is not null)
        {
            return ClanErrors.AlreadyInClan;
        }

        if (await requestRepository.HasPendingAsync(clan.Id, command.InvitedAccountId, cancellationToken))
        {
            return ClanErrors.RequestDuplicate;
        }

        var invite = new ClanJoinRequest(clan.Id, command.InvitedAccountId, ClanRequestKind.Invite, command.AccountId, membership.Now);
        await requestRepository.AddAsync(invite, cancellationToken);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? ClanSaveErrors.Map(saveResult.Error) : invite.ToDto(clan.Tag);
    }
}
