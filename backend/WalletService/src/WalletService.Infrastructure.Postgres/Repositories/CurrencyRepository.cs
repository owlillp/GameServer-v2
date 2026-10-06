using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.SharedKernel.Errors;
using WalletService.Core.Abstractions;
using WalletService.Domain.Wallets;

namespace WalletService.Infrastructure.Postgres.Repositories;

public sealed class CurrencyRepository(
    ILogger<CurrencyRepository> logger,
    WalletServiceDbContext dbContext) : ICurrencyRepository
{
    public async Task<Result<CurrencyDefinition, Error>> GetByAsync(
        Expression<Func<CurrencyDefinition, bool>> expression,
        CancellationToken cancellationToken)
    {
        try
        {
            var currency = await dbContext.Currencies
                .FirstOrDefaultAsync(expression, cancellationToken);

            return currency is not null
                ? currency
                : GeneralErrors.NotFound();
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "Operation was cancelled while getting currency");
            return GeneralErrors.Failure("Process get currency");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while getting currency");
            return GeneralErrors.Failure();
        }
    }

    public Task<Result<string, Error>> AddAsync(CurrencyDefinition currency, CancellationToken cancellationToken)
    {
        try
        {
            dbContext.Currencies.Add(currency);
            return Task.FromResult<Result<string, Error>>(currency.Code);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "Operation was cancelled while creating currency");
            return Task.FromResult<Result<string, Error>>(GeneralErrors.Failure("Process create currency"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while creating currency");
            return Task.FromResult<Result<string, Error>>(GeneralErrors.Failure());
        }
    }
}
