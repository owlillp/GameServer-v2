using ClanService.Contracts.Dtos;
using ClanService.Core.Abstractions;
using ClanService.Core.Features.Clans.Models;
using ClanService.Core.Services;
using ClanService.Domain;
using ClanService.Domain.Clans;
using ClanService.Domain.Members;
using CSharpFunctionalExtensions;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace ClanService.Core.Features.Clans.Commands;

public sealed record ChangeRoleCommand(Guid AccountId, Guid ClanId, Guid TargetAccountId, string Role) : ICommand;

public sealed record KickMemberCommand(Guid AccountId, Guid ClanId, Guid TargetAccountId) : ICommand;

public sealed record LeaveClanCommand(Guid AccountId) : ICommand;

public sealed record TransferLeadershipCommand(Guid AccountId, Guid ClanId, Guid TargetAccountId) : ICommand;

public sealed record DisbandClanCommand(Guid AccountId, Guid ClanId) : ICommand;

public sealed class ChangeRoleHandler(
    IClanEventPublisher eventPublisher,
    ClanMembership membership,
    ITransactionManager transactionManager) : ICommandHandler<ClanMemberDto, ChangeRoleCommand>
{
    public async Task<Result<ClanMemberDto, Error>> Handle(ChangeRoleCommand command, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse(command.Role, ignoreCase: true, out ClanRole newRole) || !Enum.IsDefined(newRole))
        {
            return ClanErrors.RoleInvalid;
        }

        var actorResult = await membership.GetActorAsync(command.ClanId, command.AccountId, cancellationToken);
        var targetResult = await membership.GetActorAsync(command.ClanId, command.TargetAccountId, cancellationToken);
        if (actorResult.IsFailure || targetResult.IsFailure)
        {
            return ClanErrors.NotInClan;
        }

        ClanMember target = targetResult.Value;
        if (!ClanPolicy.CanChangeRole(actorResult.Value.Role, target.Role, newRole))
        {
            return ClanErrors.Forbidden;
        }

        if (target.Role == newRole)
        {
            return target.ToDto();
        }

        ClanRole oldRole = target.Role;
        target.ChangeRole(newRole);
        await eventPublisher.PublishRoleChangedAsync(target, oldRole, command.AccountId, membership.Now);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? saveResult.Error : target.ToDto();
    }
}

public sealed class KickMemberHandler(
    ClanMembership membership,
    ITransactionManager transactionManager) : ICommandHandler<bool, KickMemberCommand>
{
    public async Task<Result<bool, Error>> Handle(KickMemberCommand command, CancellationToken cancellationToken = default)
    {
        var actorResult = await membership.GetActorAsync(command.ClanId, command.AccountId, cancellationToken);
        var targetResult = await membership.GetActorAsync(command.ClanId, command.TargetAccountId, cancellationToken);
        if (actorResult.IsFailure || targetResult.IsFailure)
        {
            return ClanErrors.NotInClan;
        }

        if (!ClanPolicy.CanKick(actorResult.Value.Role, targetResult.Value.Role))
        {
            return ClanErrors.Forbidden;
        }

        var beginResult = await transactionManager.BeginTransactionAsync(cancellationToken);
        if (beginResult.IsFailure)
        {
            return beginResult.Error;
        }

        await membership.RemoveMemberAsync(targetResult.Value, MemberLeaveReason.Kicked, cancellationToken);

        var commitResult = await transactionManager.CommitTransactionAsync(cancellationToken);
        if (commitResult.IsFailure)
        {
            return commitResult.Error;
        }

        return true;
    }
}

/// <summary>
/// A leader with other members must transfer leadership first; a leader alone disbands the clan.
/// </summary>
public sealed class LeaveClanHandler(
    IClanRepository clanRepository,
    IClanMemberRepository memberRepository,
    ClanMembership membership,
    ITransactionManager transactionManager) : ICommandHandler<bool, LeaveClanCommand>
{
    public async Task<Result<bool, Error>> Handle(LeaveClanCommand command, CancellationToken cancellationToken = default)
    {
        ClanMember? member = await memberRepository.GetAsync(command.AccountId, cancellationToken);
        if (member is null)
        {
            return ClanErrors.NotInClan;
        }

        Clan? clan = await clanRepository.GetAsync(member.ClanId, cancellationToken);
        if (clan is null)
        {
            return ClanErrors.ClanNotFound;
        }

        if (member.Role == ClanRole.Leader && clan.MemberCount > 1)
        {
            return ClanErrors.LeaderMustTransfer;
        }

        var beginResult = await transactionManager.BeginTransactionAsync(cancellationToken);
        if (beginResult.IsFailure)
        {
            return beginResult.Error;
        }

        if (member.Role == ClanRole.Leader)
        {
            await membership.DisbandAsync(clan, cancellationToken);
        }
        else
        {
            await membership.RemoveMemberAsync(member, MemberLeaveReason.Left, cancellationToken);
        }

        var commitResult = await transactionManager.CommitTransactionAsync(cancellationToken);
        if (commitResult.IsFailure)
        {
            return commitResult.Error;
        }

        return true;
    }
}

public sealed class TransferLeadershipHandler(
    IClanRepository clanRepository,
    IClanEventPublisher eventPublisher,
    ClanMembership membership,
    ITransactionManager transactionManager) : ICommandHandler<ClanDto, TransferLeadershipCommand>
{
    public async Task<Result<ClanDto, Error>> Handle(TransferLeadershipCommand command, CancellationToken cancellationToken = default)
    {
        var actorResult = await membership.GetActorAsync(command.ClanId, command.AccountId, cancellationToken);
        var targetResult = await membership.GetActorAsync(command.ClanId, command.TargetAccountId, cancellationToken);
        if (actorResult.IsFailure || targetResult.IsFailure)
        {
            return ClanErrors.NotInClan;
        }

        ClanMember actor = actorResult.Value;
        ClanMember target = targetResult.Value;
        if (!ClanPolicy.CanTransferLeadership(actor.Role) || actor.AccountId == target.AccountId)
        {
            return ClanErrors.Forbidden;
        }

        Clan? clan = await clanRepository.GetAsync(command.ClanId, cancellationToken);
        if (clan is null)
        {
            return ClanErrors.ClanNotFound;
        }

        DateTime now = membership.Now;
        ClanRole targetOldRole = target.Role;

        // Clan and member row versions make a concurrent second transfer fail instead of leaving two leaders.
        clan.TransferLeadership(target.AccountId);
        target.ChangeRole(ClanRole.Leader);
        actor.ChangeRole(ClanRole.Officer);

        await eventPublisher.PublishRoleChangedAsync(target, targetOldRole, actor.AccountId, now);
        await eventPublisher.PublishRoleChangedAsync(actor, ClanRole.Leader, actor.AccountId, now);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? saveResult.Error : clan.ToDto(membership.MaxMembers);
    }
}

public sealed class DisbandClanHandler(
    IClanRepository clanRepository,
    ClanMembership membership,
    ITransactionManager transactionManager) : ICommandHandler<bool, DisbandClanCommand>
{
    public async Task<Result<bool, Error>> Handle(DisbandClanCommand command, CancellationToken cancellationToken = default)
    {
        var actorResult = await membership.GetActorAsync(command.ClanId, command.AccountId, cancellationToken);
        if (actorResult.IsFailure)
        {
            return actorResult.Error;
        }

        if (!ClanPolicy.CanDisband(actorResult.Value.Role))
        {
            return ClanErrors.Forbidden;
        }

        Clan? clan = await clanRepository.GetAsync(command.ClanId, cancellationToken);
        if (clan is null || !clan.IsActive)
        {
            return ClanErrors.ClanNotFound;
        }

        var beginResult = await transactionManager.BeginTransactionAsync(cancellationToken);
        if (beginResult.IsFailure)
        {
            return beginResult.Error;
        }

        await membership.DisbandAsync(clan, cancellationToken);

        var commitResult = await transactionManager.CommitTransactionAsync(cancellationToken);
        if (commitResult.IsFailure)
        {
            return commitResult.Error;
        }

        return true;
    }
}
