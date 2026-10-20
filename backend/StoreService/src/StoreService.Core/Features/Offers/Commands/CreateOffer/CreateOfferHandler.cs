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

namespace StoreService.Core.Features.Offers.Commands.CreateOffer;

public sealed class CreateOfferHandler(
    IValidator<CreateOfferCommand> validator,
    IOfferRepository offerRepository,
    ICatalogClient catalogClient,
    ITransactionManager transactionManager) : ICommandHandler<OfferDto, CreateOfferCommand>
{
    public async Task<Result<OfferDto, Error>> Handle(
        CreateOfferCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        string code = command.Request.Code.Trim().ToLowerInvariant();
        if (await offerRepository.CodeExistsAsync(code, excludeOfferId: null, cancellationToken))
        {
            return StoreErrors.OfferCodeDuplicate;
        }

        var itemsResult = await OfferItemsFactory.BuildAsync(
            command.Request.Items,
            catalogClient,
            cancellationToken);

        if (itemsResult.IsFailure)
        {
            return itemsResult.Error;
        }

        var offerResult = Offer.Create(
            code,
            command.Request.DisplayName,
            command.Request.Description,
            command.Request.PriceCurrency,
            command.Request.PriceAmount,
            command.Request.StartsAt,
            command.Request.EndsAt,
            command.Request.SortOrder,
            itemsResult.Value);

        if (offerResult.IsFailure)
        {
            return offerResult.Error;
        }

        await offerRepository.AddAsync(offerResult.Value, cancellationToken);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure
            ? saveResult.Error
            : OfferMapper.ToDto(offerResult.Value);
    }
}
