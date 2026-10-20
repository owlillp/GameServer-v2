using EventService.Core.Messaging;
using Wolverine.EntityFrameworkCore;

namespace EventService.Infrastructure.Postgres;

public sealed class OutboxService(IDbContextOutbox<EventServiceDbContext> outbox) : IOutboxService
{
    public async Task PublishAsync<T>(T message)
        where T : class => await outbox.PublishAsync(message);

    public Task FlushAsync() => outbox.FlushOutgoingMessagesAsync();
}
