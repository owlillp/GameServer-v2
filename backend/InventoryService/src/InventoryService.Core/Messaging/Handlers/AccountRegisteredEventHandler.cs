using InventoryService.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Shared.IntegrationEvents.Identity.Events;

namespace InventoryService.Core.Messaging.Handlers;

public sealed class AccountRegisteredEventHandler(
    ILogger<AccountRegisteredEventHandler> logger,
    IInventoryRepository inventoryRepository)
{
    public async Task Handle(AccountRegisteredEvent @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Provisioning inventory for account {AccountId}", @event.AccountId);

        await inventoryRepository.GetOrCreateAsync(@event.AccountId, cancellationToken);

        logger.LogInformation("Inventory ready for account {AccountId}", @event.AccountId);
    }
}
