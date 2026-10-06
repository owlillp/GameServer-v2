using WalletService.Core.Messaging;
using Wolverine.EntityFrameworkCore;

namespace WalletService.Infrastructure.Postgres;

public class OutboxService(IDbContextOutbox<WalletServiceDbContext> outbox) : IOutboxService
{
    public async Task PublishAsync<T>(T message)
        where T : class => await outbox.PublishAsync(message);

    public Task FlushAsync() => outbox.FlushOutgoingMessagesAsync();
}
