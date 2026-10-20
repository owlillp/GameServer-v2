using Shared.Core.Abstractions;
using StoreService.Contracts.Requests;

namespace StoreService.Core.Features.Offers.Commands.UpdateOffer;

public sealed record UpdateOfferCommand(Guid OfferId, UpdateOfferRequest Request) : ICommand;
