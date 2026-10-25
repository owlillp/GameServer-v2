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
using Shared.SharedKernel.Errors;

namespace ClanService.Core.Features.Clans.Queries;

public sealed record GetClanQuery(Guid ClanId) : IQuery;

public sealed record GetMyClanQuery(Guid AccountId) : IQuery;

public sealed record SearchClansQuery(string? Query, bool ActiveOnly) : IQuery;

public sealed record ListClanRequestsQuery(Guid AccountId, Guid ClanId) : IQuery;

public sealed record ListMyRequestsQuery(Guid AccountId) : IQuery;

public sealed class GetClanHandler(
    IClanRepository clanRepository,
    IClanMemberRepository memberRepository,
    ClanMembership membership) : IQueryHandler<ClanDetailsDto, GetClanQuery>
{
    public async Task<Result<ClanDetailsDto, Error>> Handle(GetClanQuery query, CancellationToken cancellationToken = default)
    {
        Clan? clan = await clanRepository.GetAsync(query.ClanId, cancellationToken);
        if (clan is null)
        {
            return ClanErrors.ClanNotFound;
        }

        IReadOnlyList<ClanMember> members = await memberRepository.ListByClanAsync(clan.Id, cancellationToken);
        return new ClanDetailsDto(clan.ToDto(membership.MaxMembers), members.ToDtos());
    }
}

public sealed class GetMyClanHandler(
    IClanRepository clanRepository,
    IClanMemberRepository memberRepository,
    ClanMembership membership) : IQueryHandler<MyClanDto, GetMyClanQuery>
{
    public async Task<Result<MyClanDto, Error>> Handle(GetMyClanQuery query, CancellationToken cancellationToken = default)
    {
        ClanMember? me = await memberRepository.GetAsync(query.AccountId, cancellationToken);
        if (me is null)
        {
            return ClanErrors.NotInClan;
        }

        Clan? clan = await clanRepository.GetAsync(me.ClanId, cancellationToken);
        if (clan is null)
        {
            return ClanErrors.ClanNotFound;
        }

        IReadOnlyList<ClanMember> members = await memberRepository.ListByClanAsync(clan.Id, cancellationToken);
        return new MyClanDto(clan.ToDto(membership.MaxMembers), me.ToDto().Role, members.ToDtos());
    }
}

public sealed class SearchClansHandler(
    IClanRepository clanRepository,
    ClanMembership membership) : IQueryHandler<IReadOnlyList<ClanDto>, SearchClansQuery>
{
    private const int LIMIT = 50;

    public async Task<Result<IReadOnlyList<ClanDto>, Error>> Handle(SearchClansQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Clan> clans = await clanRepository.SearchAsync(query.Query, query.ActiveOnly, LIMIT, cancellationToken);
        return clans.Select(clan => clan.ToDto(membership.MaxMembers)).ToList();
    }
}

public sealed class ListClanRequestsHandler(
    IClanRepository clanRepository,
    IClanRequestRepository requestRepository,
    ClanMembership membership) : IQueryHandler<IReadOnlyList<ClanJoinRequestDto>, ListClanRequestsQuery>
{
    public async Task<Result<IReadOnlyList<ClanJoinRequestDto>, Error>> Handle(
        ListClanRequestsQuery query,
        CancellationToken cancellationToken = default)
    {
        var actorResult = await membership.GetActorAsync(query.ClanId, query.AccountId, cancellationToken);
        if (actorResult.IsFailure)
        {
            return actorResult.Error;
        }

        if (!ClanPolicy.CanManageRequests(actorResult.Value.Role))
        {
            return ClanErrors.Forbidden;
        }

        Clan? clan = await clanRepository.GetAsync(query.ClanId, cancellationToken);
        IReadOnlyList<ClanJoinRequest> requests = await requestRepository.ListPendingByClanAsync(query.ClanId, cancellationToken);

        return requests.Select(request => request.ToDto(clan?.Tag ?? string.Empty)).ToList();
    }
}

/// <summary>
/// Invites the player received and requests the player sent, still pending.
/// </summary>
public sealed class ListMyRequestsHandler(
    IClanRepository clanRepository,
    IClanRequestRepository requestRepository) : IQueryHandler<IReadOnlyList<ClanJoinRequestDto>, ListMyRequestsQuery>
{
    public async Task<Result<IReadOnlyList<ClanJoinRequestDto>, Error>> Handle(
        ListMyRequestsQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ClanJoinRequest> requests = await requestRepository.ListPendingByAccountAsync(query.AccountId, cancellationToken);
        var result = new List<ClanJoinRequestDto>(requests.Count);

        foreach (ClanJoinRequest request in requests)
        {
            Clan? clan = await clanRepository.GetAsync(request.ClanId, cancellationToken);
            if (clan is { IsActive: true })
            {
                result.Add(request.ToDto(clan.Tag));
            }
        }

        return result;
    }
}
