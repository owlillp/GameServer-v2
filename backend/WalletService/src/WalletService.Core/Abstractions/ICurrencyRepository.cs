using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;
using WalletService.Domain.Wallets;

namespace WalletService.Core.Abstractions;

public interface ICurrencyRepository
{
    Task<Result<CurrencyDefinition, Error>> GetByAsync(
        Expression<Func<CurrencyDefinition, bool>> expression,
        CancellationToken cancellationToken);

    Task<Result<string, Error>> AddAsync(CurrencyDefinition currency, CancellationToken cancellationToken);
}
