using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.Core.Validation;
using Shared.IntegrationEvents.Stores.Commands;
using Shared.SharedKernel.Errors;
using StoreService.Contracts.Dtos;
using StoreService.Core.Abstractions;
using CatalogService.Contracts.Communication;
using WalletService.Contracts.Communication;
using StoreService.Core.Features.Orders.Models;
using StoreService.Core.Messaging;
using StoreService.Domain;
using StoreService.Domain.Offers;
using StoreService.Domain.Orders;

namespace StoreService.Core.Features.Orders.Commands.PurchaseOffer;

public sealed class PurchaseOfferHandler(
    IValidator<PurchaseOfferCommand> validator,
    IOfferRepository offerRepository,
    IOrderRepository orderRepository,
    ICatalogClient catalogClient,
    IWalletClient walletClient,
    IOutboxService outbox,
    ITransactionManager transactionManager,
    ILogger<PurchaseOfferHandler> logger) : ICommandHandler<OrderDto, PurchaseOfferCommand>
{
    public async Task<Result<OrderDto, Error>> Handle(
        PurchaseOfferCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        string? idempotencyKey = NormalizeIdempotencyKey(command.IdempotencyKey);
        if (idempotencyKey is not null)
        {
            Order? existing = await orderRepository.FindByIdempotencyKeyAsync(
                command.AccountId,
                idempotencyKey,
                cancellationToken);

            if (existing is not null)
            {
                return OrderMapper.ToDto(existing);
            }
        }

        var offerResult = await offerRepository.GetByAsync(
            offer => offer.Id == command.Request.OfferId,
            cancellationToken);

        if (offerResult.IsFailure)
        {
            return StoreErrors.OfferNotFound;
        }

        Offer offer = offerResult.Value;
        if (!offer.IsAvailable(DateTime.UtcNow))
        {
            return StoreErrors.OfferNotAvailable;
        }

        foreach (Guid itemId in offer.Items.Select(item => item.CatalogItemId))
        {
            var catalogResult = await catalogClient.GetItemAsync(itemId, cancellationToken);
            if (catalogResult.IsFailure)
            {
                return StoreErrors.CatalogItemUnavailable(itemId);
            }
        }

        var orderResult = Order.CreatePending(command.AccountId, offer, idempotencyKey);
        if (orderResult.IsFailure)
        {
            return orderResult.Error;
        }

        Order order = orderResult.Value;
        await orderRepository.AddAsync(order, cancellationToken);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        var reserveResult = await walletClient.ReserveAsync(
            command.AccountId,
            order.Currency,
            order.TotalAmount,
            order.Id,
            order.Id.ToString(),
            cancellationToken);

        if (reserveResult.IsFailure)
        {
            string reason = Describe(reserveResult.Error);

            logger.LogWarning(
                "Wallet reservation failed for order {OrderId}: {Error}",
                order.Id,
                reason);

            order.Fail($"Wallet reservation failed: {reason}");
            await transactionManager.SaveChangesAsync(cancellationToken);
            return OrderMapper.ToDto(order);
        }

        var reservedResult = order.MarkReserved(Guid.Parse(reserveResult.Value));
        if (reservedResult.IsFailure)
        {
            return reservedResult.Error;
        }

        var grantRequestedResult = order.MarkGrantRequested();
        if (grantRequestedResult.IsFailure)
        {
            return grantRequestedResult.Error;
        }

        await outbox.PublishAsync(new GrantPurchaseItemsCommand(
            order.Id,
            order.AccountId,
            [.. order.Items.Select(item => new GrantPurchaseItem(
                item.CatalogItemId,
                item.Code,
                item.Kind,
                item.Slot,
                item.Quantity))],
            DateTime.UtcNow));

        var finalSaveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (finalSaveResult.IsFailure)
        {
            return finalSaveResult.Error;
        }

        return OrderMapper.ToDto(order);
    }

    private static string Describe(Error error) =>
        error.Messages.Count == 0
            ? "Wallet is unavailable"
            : string.Join("; ", error.Messages.Select(message => message.Message));

    private static string? NormalizeIdempotencyKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        string normalized = key.Trim();
        return normalized.Length > StoreConstants.IDEMPOTENCY_KEY_MAX_LENGTH
            ? normalized[..StoreConstants.IDEMPOTENCY_KEY_MAX_LENGTH]
            : normalized;
    }
}
