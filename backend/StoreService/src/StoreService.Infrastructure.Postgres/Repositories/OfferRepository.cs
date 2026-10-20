using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Shared.SharedKernel.Errors;
using StoreService.Core.Abstractions;
using StoreService.Domain.Offers;

namespace StoreService.Infrastructure.Postgres.Repositories;

public sealed class OfferRepository(StoreServiceDbContext dbContext) : IOfferRepository
{
    public async Task<Result<Offer, Error>> GetByAsync(
        Expression<Func<Offer, bool>> expression,
        CancellationToken cancellationToken)
    {
        Offer? offer = await dbContext.Offers
            .Include(entity => entity.Items)
            .FirstOrDefaultAsync(expression, cancellationToken);

        return offer is not null ? offer : GeneralErrors.NotFound();
    }

    public async Task<IReadOnlyList<Offer>> ListAsync(bool includeArchived, CancellationToken cancellationToken) =>
        await dbContext.Offers
            .Include(offer => offer.Items)
            .Where(offer => includeArchived || offer.Status != OfferStatus.ARCHIVED)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

    public Task<bool> CodeExistsAsync(string code, Guid? excludeOfferId, CancellationToken cancellationToken) =>
        dbContext.Offers.AnyAsync(
            offer => offer.Code == code && (excludeOfferId == null || offer.Id != excludeOfferId),
            cancellationToken);

    public Task AddAsync(Offer offer, CancellationToken cancellationToken)
    {
        dbContext.Offers.Add(offer);
        return Task.CompletedTask;
    }
}
