using CatalogService.Core.Messaging;
using Wolverine.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Postgres;

public class OutboxService(IDbContextOutbox<CatalogServiceDbContext> outbox) : IOutboxService
{
    public async Task PublishAsync<T>(T message)
        where T : class => await outbox.PublishAsync(message);

    public Task FlushAsync() => outbox.FlushOutgoingMessagesAsync();
}
