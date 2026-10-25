using ClanWarsService.Contracts;
using ClanWarsService.Core.Abstractions;
using ClanWarsService.Core.Configuration;
using ClanWarsService.Core.Services;
using ClanWarsService.Domain;
using ClanWarsService.Domain.Roster;
using ClanWarsService.Domain.Seasons;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace ClanWarsService.Core.Features;

public sealed record GetCurrentSeasonQuery : IQuery;

public sealed record GetLeaderboardQuery(Guid SeasonId, int Limit) : IQuery;

public sealed record GetClanStandingQuery(Guid SeasonId, Guid ClanId, int Neighbors) : IQuery;

public sealed record GetMyStandingQuery(Guid SeasonId, Guid AccountId, int Neighbors) : IQuery;

public sealed record GetMemberContributionsQuery(Guid SeasonId, Guid ClanId) : IQuery;

public sealed class GetCurrentSeasonHandler(ISeasonRepository seasonRepository) : IQueryHandler<SeasonDto, GetCurrentSeasonQuery>
{
    public async Task<Result<SeasonDto, Error>> Handle(GetCurrentSeasonQuery query, CancellationToken cancellationToken = default)
    {
        Season? season = await seasonRepository.GetCurrentAsync(cancellationToken);
        return season is null ? ClanWarsErrors.NoCurrentSeason : season.ToDto();
    }
}

public sealed class GetLeaderboardHandler(
    LeaderboardReader reader,
    IOptionsMonitor<ClanWarsOptions> options) : IQueryHandler<IReadOnlyList<LeaderboardRowDto>, GetLeaderboardQuery>
{
    public async Task<Result<IReadOnlyList<LeaderboardRowDto>, Error>> Handle(GetLeaderboardQuery query, CancellationToken cancellationToken = default)
    {
        int limit = Math.Clamp(query.Limit, 1, options.CurrentValue.MaxLeaderboardLimit);
        return Result.Success<IReadOnlyList<LeaderboardRowDto>, Error>(
            await reader.GetRangeAsync(query.SeasonId, 0, limit, cancellationToken));
    }
}

public sealed class GetClanStandingHandler(
    LeaderboardReader reader,
    IOptionsMonitor<ClanWarsOptions> options) : IQueryHandler<ClanStandingDto, GetClanStandingQuery>
{
    public Task<Result<ClanStandingDto, Error>> Handle(GetClanStandingQuery query, CancellationToken cancellationToken = default) =>
        reader.GetStandingAsync(
            query.SeasonId,
            query.ClanId,
            Math.Clamp(query.Neighbors, 0, options.CurrentValue.MaxNeighbors),
            cancellationToken);
}

/// <summary>
/// The player's clan comes from the roster projection, not from ClanService.
/// </summary>
public sealed class GetMyStandingHandler(
    IRosterRepository rosterRepository,
    LeaderboardReader reader,
    IOptionsMonitor<ClanWarsOptions> options) : IQueryHandler<ClanStandingDto, GetMyStandingQuery>
{
    public async Task<Result<ClanStandingDto, Error>> Handle(GetMyStandingQuery query, CancellationToken cancellationToken = default)
    {
        RosterEntry? entry = await rosterRepository.GetEntryAsync(query.AccountId, cancellationToken);
        if (entry is null)
        {
            return ClanWarsErrors.NotInClan;
        }

        return await reader.GetStandingAsync(
            query.SeasonId,
            entry.ClanId,
            Math.Clamp(query.Neighbors, 0, options.CurrentValue.MaxNeighbors),
            cancellationToken);
    }
}

public sealed class GetMemberContributionsHandler(IScoreRepository scoreRepository)
    : IQueryHandler<IReadOnlyList<MemberContributionDto>, GetMemberContributionsQuery>
{
    public async Task<Result<IReadOnlyList<MemberContributionDto>, Error>> Handle(
        GetMemberContributionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var stats = await scoreRepository.ListMemberStatsAsync(query.SeasonId, query.ClanId, cancellationToken);
        return stats
            .OrderByDescending(entry => entry.Points)
            .Select(entry => new MemberContributionDto(entry.AccountId, entry.Points, entry.Battles, entry.Wins))
            .ToList();
    }
}
