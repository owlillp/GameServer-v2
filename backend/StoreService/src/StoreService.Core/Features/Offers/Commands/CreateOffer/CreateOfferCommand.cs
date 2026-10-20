using Shared.Core.Abstractions;
using StoreService.Contracts.Requests;

namespace StoreService.Core.Features.Offers.Commands.CreateOffer;

public sealed record CreateOfferCommand(CreateOfferRequest Request) : ICommand;
