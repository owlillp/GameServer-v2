using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;
using WalletService.Contracts.Dtos;
using WalletService.Core.Abstractions;
using WalletService.Domain.Wallets;

namespace WalletService.Core.Features.Currencies.Queries.GetCurrencies;

public sealed class GetCurrenciesHandler(IReadDbContext dbContext)
    : IQueryHandler<IReadOnlyList<CurrencyDto>, GetCurrenciesQuery>
{
    public async Task<Result<IReadOnlyList<CurrencyDto>, Error>> Handle(
        GetCurrenciesQuery query,
        CancellationToken cancellationToken = new ())
    {
        List<CurrencyDefinition> currencies = await dbContext.CurrenciesRead
            .AsNoTracking()
            .Where(currency => currency.IsActive)
            .OrderBy(currency => currency.Code)
            .ToListAsync(cancellationToken);

        return currencies.ConvertAll(CurrencyMapper.ToDto);
    }
}
