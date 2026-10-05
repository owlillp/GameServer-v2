using Microsoft.Extensions.Logging;
using Shared.Core.Database;
using Shared.IntegrationEvents.Identity.Events;
using WalletService.Core.Abstractions;
using WalletService.Domain.Wallets;

namespace WalletService.Core.Messaging.Handlers;

/// <summary>
/// Провижининг кошелька при регистрации аккаунта. Идемпотентен по account_id
/// (PK = account_id): повторная доставка события не создаёт дубликат.
/// </summary>
public sealed class AccountRegisteredEventHandler(
    ILogger<AccountRegisteredEventHandler> logger,
    IWalletRepository walletRepository,
    ITransactionManager transactionManager)
{
    public async Task Handle(AccountRegisteredEvent @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received AccountRegisteredEvent for account: {AccountId}", @event.AccountId);

        var walletResult = Wallet.Create(@event.AccountId);
        if (walletResult.IsFailure)
        {
            logger.LogError(
                "Failed to create wallet: {AccountId}, error: {Error}",
                @event.AccountId,
                walletResult.Error);

            return;
        }

        var addResult = await walletRepository.AddAsync(walletResult.Value, cancellationToken);
        if (addResult.IsFailure)
        {
            logger.LogError(
                "Failed to create wallet: {AccountId}, error: {Error}",
                @event.AccountId,
                addResult.Error);

            return;
        }

        var saveChangesResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveChangesResult.IsFailure)
        {
            logger.LogError(
                "Failed to create wallet: {AccountId}, error: {Error}",
                @event.AccountId,
                saveChangesResult.Error);

            return;
        }

        logger.LogInformation("Wallet created: {WalletId}", walletResult.Value.Id);
    }
}
