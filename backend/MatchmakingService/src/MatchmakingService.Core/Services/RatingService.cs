using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Configuration;
using MatchmakingService.Domain.Matches;
using MatchmakingService.Domain.Ratings;
using Microsoft.Extensions.Options;

namespace MatchmakingService.Core.Services;

public sealed class RatingService(
    IRatingRepository ratingRepository,
    IOptionsMonitor<MatchmakingOptions> options)
{
    public async Task<int> GetAverageAsync(IReadOnlyCollection<Guid> accountIds, CancellationToken cancellationToken)
    {
        if (accountIds.Count == 0)
        {
            return PlayerRating.DEFAULT_RATING;
        }

        IReadOnlyList<PlayerRating> ratings = await ratingRepository.ListAsync(accountIds, cancellationToken);
        var byAccount = ratings.ToDictionary(rating => rating.AccountId, rating => rating.Rating);

        return (int)Math.Round(accountIds.Average(id => byAccount.GetValueOrDefault(id, PlayerRating.DEFAULT_RATING)));
    }

    /// <summary>
    /// Inside the finalization transaction: both sides play as their average rating, every member
    /// gets the side's change. External matches (clan battles) do not change ratings.
    /// </summary>
    public async Task ApplyAsync(Match match, DateTime now, CancellationToken cancellationToken)
    {
        MatchmakingOptions current = options.CurrentValue;
        if (!current.RatingEnabled || match.IsExternal || match.Parties.Count != 2)
        {
            return;
        }

        Guid[] accountIds = [.. match.Participants.Select(participant => participant.AccountId)];
        Dictionary<Guid, PlayerRating> ratings = (await ratingRepository.ListAsync(accountIds, cancellationToken))
            .ToDictionary(rating => rating.AccountId);

        foreach (Guid accountId in accountIds.Where(id => !ratings.ContainsKey(id)))
        {
            var rating = new PlayerRating(accountId, now);
            ratings[accountId] = rating;
            await ratingRepository.AddAsync(rating, cancellationToken);
        }

        Guid sideA = match.Parties[0].PartyId;
        Guid sideB = match.Parties[1].PartyId;
        Guid[] teamA = [.. match.Participants.Where(participant => participant.PartyId == sideA).Select(participant => participant.AccountId)];
        Guid[] teamB = [.. match.Participants.Where(participant => participant.PartyId == sideB).Select(participant => participant.AccountId)];

        double ratingA = teamA.Average(id => ratings[id].Rating);
        double ratingB = teamB.Average(id => ratings[id].Rating);

        double scoreA = 0.5;
        if (match.WinnerPartyId is not null)
        {
            scoreA = match.WinnerPartyId == sideA ? 1 : 0;
        }

        int deltaA = Elo.Delta(ratingA, ratingB, scoreA, current.RatingKFactor);
        int deltaB = Elo.Delta(ratingB, ratingA, 1 - scoreA, current.RatingKFactor);

        foreach (Guid id in teamA)
        {
            ratings[id].Apply(deltaA, now);
        }

        foreach (Guid id in teamB)
        {
            ratings[id].Apply(deltaB, now);
        }
    }
}
