using StoreService.Core.Messaging;
using Wolverine.EntityFrameworkCore;

namespace StoreService.Infrastructure.Postgres;

public sealed class OutboxService(IDbContextOutbox<StoreServiceDbContext> outbox) : IOutboxService
{
    public async Task PublishAsync<T>(T message)
        where T : class => await outbox.PublishAsync(message);

    public Task FlushAsync() => outbox.FlushOutgoingMessagesAsync();
}
