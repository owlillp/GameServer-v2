using Shared.Core.Abstractions;

namespace StoreService.Core.Features.Offers.Commands.ArchiveOffer;

public sealed record ArchiveOfferCommand(Guid OfferId) : ICommand;
