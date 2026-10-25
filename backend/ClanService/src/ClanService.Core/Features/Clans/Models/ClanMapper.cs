using ClanService.Contracts.Dtos;
using ClanService.Domain.Clans;
using ClanService.Domain.Members;
using ClanService.Domain.Points;
using ClanService.Domain.Requests;

namespace ClanService.Core.Features.Clans.Models;

public static class ClanMapper
{
    public static ClanDto ToDto(this Clan clan, int maxMembers) =>
        new(
            clan.Id,
            clan.Tag,
            clan.Name,
            clan.Description,
            clan.LeaderAccountId,
            clan.MemberCount,
            maxMembers,
            clan.ClanPoints,
            clan.Status.ToString().ToUpperInvariant(),
            clan.CreatedAt);

    public static ClanMemberDto ToDto(this ClanMember member) =>
        new(member.AccountId, member.Role.ToString().ToUpperInvariant(), member.JoinedAt);

    public static ClanJoinRequestDto ToDto(this ClanJoinRequest request, string clanTag) =>
        new(
            request.Id,
            request.ClanId,
            clanTag,
            request.AccountId,
            request.Kind.ToString().ToUpperInvariant(),
            request.Status.ToString().ToUpperInvariant(),
            request.CreatedBy,
            request.CreatedAt);

    public static ClanPointsEntryDto ToDto(this ClanPointsEntry entry) =>
        new(entry.Id, entry.OperationId, entry.Amount, entry.Reason, entry.ActorId, entry.Comment, entry.CreatedAt);

    public static IReadOnlyList<ClanMemberDto> ToDtos(this IEnumerable<ClanMember> members) =>
        [.. members
            .OrderByDescending(member => member.Role)
            .ThenBy(member => member.JoinedAt)
            .Select(member => member.ToDto())];
}
