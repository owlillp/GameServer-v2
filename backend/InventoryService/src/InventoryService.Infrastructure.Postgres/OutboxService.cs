using InventoryService.Core.Messaging;
using Wolverine.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Postgres;

public class OutboxService(IDbContextOutbox<InventoryServiceDbContext> outbox) : IOutboxService
{
    public async Task PublishAsync<T>(T message)
        where T : class => await outbox.PublishAsync(message);

    public Task FlushAsync() => outbox.FlushOutgoingMessagesAsync();
}
