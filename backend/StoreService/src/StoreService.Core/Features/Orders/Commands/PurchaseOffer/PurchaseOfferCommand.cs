using StoreService.Contracts.Requests;
using Shared.Core.Abstractions;

namespace StoreService.Core.Features.Orders.Commands.PurchaseOffer;

public sealed record PurchaseOfferCommand(
    Guid AccountId,
    string? IdempotencyKey,
    PurchaseOfferRequest Request) : ICommand;
