using CSharpFunctionalExtensions;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;
using StoreService.Contracts.Dtos;
using StoreService.Core.Abstractions;
using StoreService.Core.Features.Offers.Models;
using StoreService.Domain.Offers;

namespace StoreService.Core.Features.Offers.Queries.GetOffers;

public sealed class GetOffersHandler(IOfferRepository offerRepository)
    : IQueryHandler<IReadOnlyList<OfferDto>, GetOffersQuery>
{
    public async Task<Result<IReadOnlyList<OfferDto>, Error>> Handle(
        GetOffersQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Offer> offers = await offerRepository.ListAsync(
            query.IncludeArchived,
            cancellationToken);

        DateTime now = DateTime.UtcNow;

        return offers
            .Where(offer => query.IncludeArchived || offer.IsAvailable(now))
            .OrderBy(offer => offer.SortOrder)
            .ThenBy(offer => offer.DisplayName, StringComparer.Ordinal)
            .Select(OfferMapper.ToDto)
            .ToList();
    }
}
