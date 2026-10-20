namespace StoreService.Core.Messaging;

public interface IOutboxService
{
    Task PublishAsync<T>(T message)
        where T : class;

    Task FlushAsync();
}
