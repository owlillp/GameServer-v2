using FluentValidation;
using StoreService.Domain;

namespace StoreService.Core.Features.Offers.Commands.UpdateOffer;

public sealed class UpdateOfferValidator : AbstractValidator<UpdateOfferCommand>
{
    public UpdateOfferValidator()
    {
        RuleFor(command => command.OfferId).NotEmpty();

        RuleFor(command => command.Request.DisplayName)
            .NotEmpty()
            .MaximumLength(StoreConstants.DISPLAY_NAME_MAX_LENGTH);

        RuleFor(command => command.Request.Description)
            .MaximumLength(StoreConstants.DESCRIPTION_MAX_LENGTH);

        RuleFor(command => command.Request.PriceCurrency)
            .NotEmpty()
            .MaximumLength(StoreConstants.CURRENCY_MAX_LENGTH);

        RuleFor(command => command.Request.PriceAmount)
            .GreaterThan(0);

        RuleFor(command => command.Request.Items)
            .NotEmpty();

        RuleForEach(command => command.Request.Items).ChildRules(item =>
        {
            item.RuleFor(request => request.CatalogItemId).NotEmpty();
            item.RuleFor(request => request.Quantity).GreaterThan(0);
        });
    }
}
