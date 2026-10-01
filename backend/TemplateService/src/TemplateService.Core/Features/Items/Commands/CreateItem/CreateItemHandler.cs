using TemplateService.Contracts.Items;
using TemplateService.Core.Abstractions;
using TemplateService.Domain;
using CSharpFunctionalExtensions;
using FluentValidation;
using Shared.Authentication.Authentication.UserScope;
using Shared.Core.Abstractions;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;

namespace TemplateService.Core.Features.Items.Commands.CreateItem;

public sealed class CreateItemHandler(
    IValidator<CreateItemCommand> validator,
    IItemRepository repository,
    UserScopedData currentUser) : ICommandHandler<ItemDto, CreateItemCommand>
{
    public async Task<Result<ItemDto, Error>> Handle(
        CreateItemCommand command,
        CancellationToken cancellationToken = new ())
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        Guid userId = currentUser.RequireId();

        var item = new Item(command.Request.Name, command.Request.Description, userId);
        await repository.AddAsync(item, cancellationToken);

        return new ItemDto(
            item.Id,
            item.Name,
            item.Description,
            item.OwnerId,
            item.CreatedAt,
            item.UpdatedAt);
    }
}
