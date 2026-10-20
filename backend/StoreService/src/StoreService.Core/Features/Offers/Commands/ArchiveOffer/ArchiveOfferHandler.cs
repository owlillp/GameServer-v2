using CSharpFunctionalExtensions;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;
using StoreService.Contracts.Dtos;
using StoreService.Core.Abstractions;
using StoreService.Core.Features.Offers.Models;
using StoreService.Domain;

namespace StoreService.Core.Features.Offers.Commands.ArchiveOffer;

public sealed class ArchiveOfferHandler(
    IOfferRepository offerRepository,
    ITransactionManager transactionManager) : ICommandHandler<OfferDto, ArchiveOfferCommand>
{
    public async Task<Result<OfferDto, Error>> Handle(
        ArchiveOfferCommand command,
        CancellationToken cancellationToken = default)
    {
        var offerResult = await offerRepository.GetByAsync(
            offer => offer.Id == command.OfferId,
            cancellationToken);

        if (offerResult.IsFailure)
        {
            return StoreErrors.OfferNotFound;
        }

        var archiveResult = offerResult.Value.Archive();
        if (archiveResult.IsFailure)
        {
            return archiveResult.Error;
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure
            ? saveResult.Error
            : OfferMapper.ToDto(offerResult.Value);
    }
}
