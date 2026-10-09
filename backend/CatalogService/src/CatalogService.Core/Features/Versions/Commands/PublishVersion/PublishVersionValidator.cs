using FluentValidation;

namespace CatalogService.Core.Features.Versions.Commands.PublishVersion;

public sealed class PublishVersionValidator : AbstractValidator<PublishVersionCommand>
{
    public PublishVersionValidator()
    {
        RuleFor(command => command.Request.Note)
            .MaximumLength(512);
    }
}
