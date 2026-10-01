using TemplateService.Contracts.Items;
using TemplateService.Core.Abstractions;
using TemplateService.Core.Authorization;
using TemplateService.Domain;
using CSharpFunctionalExtensions;
using FluentValidation;
using Shared.Authentication.Authentication.UserScope;
using Shared.Core.Abstractions;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;

namespace TemplateService.Core.Features.Items.Commands.UpdateItem;

public sealed class UpdateItemHandler(
    IValidator<UpdateItemCommand> validator,
    IItemRepository repository,
    UserScopedData currentUser) : ICommandHandler<ItemDto, UpdateItemCommand>
{
    public async Task<Result<ItemDto, Error>> Handle(
        UpdateItemCommand command,
        CancellationToken cancellationToken = new ())
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        Item? item = await repository.GetByIdAsync(command.ItemId, cancellationToken);
        if (item is null)
        {
            return GeneralErrors.NotFound(command.ItemId, "item");
        }

        Guid userId = currentUser.RequireId();

        // Редактировать может владелец или модератор/админ (permission items.manage).
        if (!item.IsOwnedBy(userId) && !currentUser.HasPermission(TemplatePermissions.ITEMS_MANAGE))
        {
            return GeneralErrors.Forbidden();
        }

        item.Update(command.Request.Name, command.Request.Description);
        await repository.SaveChangesAsync(cancellationToken);

        return new ItemDto(
            item.Id,
            item.Name,
            item.Description,
            item.OwnerId,
            item.CreatedAt,
            item.UpdatedAt);
    }
}
