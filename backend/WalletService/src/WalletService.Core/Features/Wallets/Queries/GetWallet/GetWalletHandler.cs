using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;
using WalletService.Contracts.Dtos;
using WalletService.Core.Abstractions;
using WalletService.Core.Features.Wallets.Models;

namespace WalletService.Core.Features.Wallets.Queries.GetWallet;

public sealed class GetWalletHandler(IReadDbContext dbContext) : IQueryHandler<WalletDto, GetWalletQuery>
{
    public async Task<Result<WalletDto, Error>> Handle(
        GetWalletQuery query,
        CancellationToken cancellationToken = new ())
    {
        var wallet = await dbContext.WalletsRead
            .AsNoTracking()
            .Include(w => w.Balances)
            .FirstOrDefaultAsync(w => w.Id == query.WalletId, cancellationToken);

        if (wallet is null)
        {
            return GeneralErrors.NotFound(query.WalletId, entityName: "wallet");
        }

        return WalletMapper.ToDto(wallet);
    }
}
