using MatchmakingService.Core.Messaging;
using Wolverine.EntityFrameworkCore;

namespace MatchmakingService.Infrastructure.Postgres;

public sealed class OutboxService(IDbContextOutbox<MatchmakingServiceDbContext> outbox) : IOutboxService
{
    public async Task PublishAsync<T>(T message)
        where T : class => await outbox.PublishAsync(message);

    public Task FlushAsync() => outbox.FlushOutgoingMessagesAsync();
}
