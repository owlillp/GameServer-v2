using Shared.Core.Abstractions;

namespace StoreService.Core.Features.Offers.Commands.ActivateOffer;

public sealed record ActivateOfferCommand(Guid OfferId) : ICommand;
