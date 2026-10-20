using Shared.Core.Abstractions;

namespace StoreService.Core.Features.Offers.Queries.GetOffers;

public sealed record GetOffersQuery(bool IncludeArchived = false) : IQuery;
