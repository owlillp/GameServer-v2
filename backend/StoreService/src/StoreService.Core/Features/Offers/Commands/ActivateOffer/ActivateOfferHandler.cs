using CSharpFunctionalExtensions;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;
using StoreService.Contracts.Dtos;
using StoreService.Core.Abstractions;
using StoreService.Core.Features.Offers.Models;
using StoreService.Domain;

namespace StoreService.Core.Features.Offers.Commands.ActivateOffer;

public sealed class ActivateOfferHandler(
    IOfferRepository offerRepository,
    ITransactionManager transactionManager) : ICommandHandler<OfferDto, ActivateOfferCommand>
{
    public async Task<Result<OfferDto, Error>> Handle(
        ActivateOfferCommand command,
        CancellationToken cancellationToken = default)
    {
        var offerResult = await offerRepository.GetByAsync(
            offer => offer.Id == command.OfferId,
            cancellationToken);

        if (offerResult.IsFailure)
        {
            return StoreErrors.OfferNotFound;
        }

        var activateResult = offerResult.Value.Activate();
        if (activateResult.IsFailure)
        {
            return activateResult.Error;
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure
            ? saveResult.Error
            : OfferMapper.ToDto(offerResult.Value);
    }
}
