using Shared.Core.Abstractions;

namespace StoreService.Core.Features.Offers.Queries.GetOffer;

public sealed record GetOfferQuery(Guid OfferId, bool IncludeUnavailable = false) : IQuery;
