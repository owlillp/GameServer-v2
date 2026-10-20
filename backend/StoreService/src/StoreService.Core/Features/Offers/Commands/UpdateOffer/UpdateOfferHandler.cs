using CSharpFunctionalExtensions;
using FluentValidation;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;
using StoreService.Contracts.Dtos;
using StoreService.Core.Abstractions;
using CatalogService.Contracts.Communication;
using StoreService.Core.Features.Offers.Models;
using StoreService.Domain;
using StoreService.Domain.Offers;

namespace StoreService.Core.Features.Offers.Commands.UpdateOffer;

public sealed class UpdateOfferHandler(
    IValidator<UpdateOfferCommand> validator,
    IOfferRepository offerRepository,
    ICatalogClient catalogClient,
    ITransactionManager transactionManager) : ICommandHandler<OfferDto, UpdateOfferCommand>
{
    public async Task<Result<OfferDto, Error>> Handle(
        UpdateOfferCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        var offerResult = await offerRepository.GetByAsync(
            offer => offer.Id == command.OfferId,
            cancellationToken);

        if (offerResult.IsFailure)
        {
            return StoreErrors.OfferNotFound;
        }

        var itemsResult = await OfferItemsFactory.BuildAsync(
            command.Request.Items,
            catalogClient,
            cancellationToken);

        if (itemsResult.IsFailure)
        {
            return itemsResult.Error;
        }

        var updateResult = offerResult.Value.Update(
            command.Request.DisplayName,
            command.Request.Description,
            command.Request.PriceCurrency,
            command.Request.PriceAmount,
            command.Request.StartsAt,
            command.Request.EndsAt,
            command.Request.SortOrder,
            itemsResult.Value);

        if (updateResult.IsFailure)
        {
            return updateResult.Error;
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure
            ? saveResult.Error
            : OfferMapper.ToDto(offerResult.Value);
    }
}
