using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;
using StoreService.Domain.Offers;

namespace StoreService.Core.Abstractions;

public interface IOfferRepository
{
    Task<Result<Offer, Error>> GetByAsync(
        Expression<Func<Offer, bool>> expression,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Offer>> ListAsync(bool includeArchived, CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(string code, Guid? excludeOfferId, CancellationToken cancellationToken);

    Task AddAsync(Offer offer, CancellationToken cancellationToken);
}
