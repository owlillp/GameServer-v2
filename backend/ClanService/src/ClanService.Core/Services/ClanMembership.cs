using ClanService.Core.Abstractions;
using ClanService.Core.Configuration;
using ClanService.Domain;
using ClanService.Domain.Clans;
using ClanService.Domain.Members;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;
using Shared.SharedKernel.Errors;

namespace ClanService.Core.Services;

/// <summary>
/// Joining and leaving, shared by every command that changes the roster.
/// Must run inside a transaction: the slot reservation is an immediate SQL update.
/// The one-clan-per-player rule is the primary key of clan_members; a race ends in 23505.
/// </summary>
public sealed class ClanMembership(
    IClanRepository clanRepository,
    IClanMemberRepository memberRepository,
    IClanEventPublisher eventPublisher,
    IOptionsMonitor<ClanOptions> options,
    TimeProvider timeProvider)
{
    public DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    public int MaxMembers => options.CurrentValue.MaxMembers;

    /// <summary>
    /// Checks that apply before a player may enter any clan.
    /// </summary>
    public async Task<UnitResult<Error>> EnsureCanJoinAsync(Guid accountId, CancellationToken cancellationToken)
    {
        if (await memberRepository.GetAsync(accountId, cancellationToken) is not null)
        {
            return ClanErrors.AlreadyInClan;
        }

        DateTime? lastLeave = await memberRepository.GetLastVoluntaryLeaveAsync(accountId, cancellationToken);
        TimeSpan cooldown = TimeSpan.FromHours(options.CurrentValue.JoinCooldownHours);

        return lastLeave is not null && Now - lastLeave.Value < cooldown
            ? ClanErrors.JoinCooldown
            : UnitResult.Success<Error>();
    }

    public async Task<Result<ClanMember, Error>> AddMemberAsync(
        Clan clan,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (!clan.IsActive)
        {
            return ClanErrors.ClanDisbanded;
        }

        var canJoin = await EnsureCanJoinAsync(accountId, cancellationToken);
        if (canJoin.IsFailure)
        {
            return canJoin.Error;
        }

        if (!await clanRepository.TryReserveSlotAsync(clan.Id, MaxMembers, cancellationToken))
        {
            return ClanErrors.ClanFull;
        }

        var member = new ClanMember(clan.Id, accountId, ClanRole.Member, Now);
        await memberRepository.AddAsync(member, cancellationToken);
        await eventPublisher.PublishMemberJoinedAsync(member);

        return member;
    }

    public async Task RemoveMemberAsync(ClanMember member, MemberLeaveReason reason, CancellationToken cancellationToken)
    {
        DateTime now = Now;

        memberRepository.Remove(member);
        await memberRepository.AddHistoryAsync(new ClanMemberHistory(member, reason, now), cancellationToken);
        await clanRepository.ReleaseSlotAsync(member.ClanId, cancellationToken);
        await eventPublisher.PublishMemberLeftAsync(member, reason, now);
    }

    /// <summary>
    /// The acting player as a member of <paramref name="clanId"/>.
    /// </summary>
    public async Task<Result<ClanMember, Error>> GetActorAsync(
        Guid clanId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        ClanMember? member = await memberRepository.GetAsync(accountId, cancellationToken);

        return member is not null && member.ClanId == clanId ? member : ClanErrors.NotInClan;
    }

    public async Task DisbandAsync(Clan clan, CancellationToken cancellationToken)
    {
        DateTime now = Now;
        IReadOnlyList<ClanMember> members = await memberRepository.ListByClanAsync(clan.Id, cancellationToken);

        foreach (ClanMember member in members)
        {
            memberRepository.Remove(member);
            await memberRepository.AddHistoryAsync(
                new ClanMemberHistory(member, MemberLeaveReason.Disbanded, now),
                cancellationToken);
        }

        clan.Disband(now);
        await eventPublisher.PublishClanDisbandedAsync(clan, [.. members.Select(member => member.AccountId)]);
    }
}
