using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StoreService.Core.Abstractions;
using WalletService.Contracts.Communication;
using StoreService.Core.Configuration;
using StoreService.Domain.Orders;

namespace StoreService.Core.Services;

public sealed class OrderTimeoutService(
    IServiceScopeFactory scopeFactory,
    IOptions<StoreOptions> options,
    ILogger<OrderTimeoutService> logger) : BackgroundService
{
    private static readonly TimeSpan SWEEP_INTERVAL = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SWEEP_INTERVAL);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }

                await SweepAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Purchase order timeout sweep failed");
            }
        }
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var walletClient = scope.ServiceProvider.GetRequiredService<IWalletClient>();

        DateTime createdBefore = DateTime.UtcNow.AddMinutes(-options.Value.OrderTimeoutMinutes);
        IReadOnlyList<Order> orders = await orderRepository.ListPendingAsync(createdBefore, cancellationToken);

        if (orders.Count == 0)
        {
            return;
        }

        foreach (Order order in orders)
        {
            if (order.HoldId is not null)
            {
                var releaseResult = await walletClient.ReleaseAsync(order.HoldId.Value, cancellationToken);
                if (releaseResult.IsFailure)
                {
                    logger.LogWarning(
                        "Failed to release hold for timed out order {OrderId}: {Error}",
                        order.Id,
                        releaseResult.Error.ToString());

                    continue;
                }
            }

            order.Fail("Purchase timed out");
            logger.LogInformation("Purchase order {OrderId} marked as failed by timeout", order.Id);
        }

        var transactionManager = scope.ServiceProvider.GetRequiredService<Shared.Core.Database.ITransactionManager>();
        await transactionManager.SaveChangesAsync(cancellationToken);
    }
}
