using CSharpFunctionalExtensions;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;
using StoreService.Contracts.Dtos;
using StoreService.Core.Abstractions;
using StoreService.Core.Features.Offers.Models;
using StoreService.Domain;

namespace StoreService.Core.Features.Offers.Queries.GetOffer;

public sealed class GetOfferHandler(IOfferRepository offerRepository)
    : IQueryHandler<OfferDto, GetOfferQuery>
{
    public async Task<Result<OfferDto, Error>> Handle(
        GetOfferQuery query,
        CancellationToken cancellationToken = default)
    {
        var offerResult = await offerRepository.GetByAsync(
            offer => offer.Id == query.OfferId,
            cancellationToken);

        if (offerResult.IsFailure)
        {
            return StoreErrors.OfferNotFound;
        }

        if (!query.IncludeUnavailable && !offerResult.Value.IsAvailable(DateTime.UtcNow))
        {
            return StoreErrors.OfferNotAvailable;
        }

        return OfferMapper.ToDto(offerResult.Value);
    }
}
