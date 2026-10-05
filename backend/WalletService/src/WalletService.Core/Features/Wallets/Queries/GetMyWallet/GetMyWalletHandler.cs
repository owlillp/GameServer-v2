using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Shared.Authentication.Authentication.UserScope;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;
using WalletService.Contracts.Dtos;
using WalletService.Core.Abstractions;

namespace WalletService.Core.Features.Wallets.Queries.GetMyWallet;

public sealed class GetMyWalletHandler(
    IReadDbContext dbContext,
    UserScopedData userScope) : IQueryHandler<WalletDto, GetMyWalletQuery>
{
    public async Task<Result<WalletDto, Error>> Handle(
        GetMyWalletQuery query,
        CancellationToken cancellationToken = new ())
    {
        Guid? userId = userScope.Id;
        if (userId is null)
        {
            return Error.Authentication("authentication.required", "User identity is not available");
        }

        var wallet = await dbContext.WalletsRead
            .AsNoTracking()
            .Include(w => w.Balances)
            .FirstOrDefaultAsync(w => w.Id == userId.Value, cancellationToken);

        if (wallet is null)
        {
            return GeneralErrors.NotFound(userId.Value, entityName: "wallet");
        }

        return WalletMapper.ToDto(wallet);
    }
}
