using TemplateService.Core.Abstractions;
using TemplateService.Core.Authorization;
using TemplateService.Domain;
using CSharpFunctionalExtensions;
using Shared.Authentication.Authentication.UserScope;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace TemplateService.Core.Features.Items.Commands.DeleteItem;

public sealed class DeleteItemHandler(
    IItemRepository repository,
    UserScopedData currentUser) : ICommandHandler<DeleteItemCommand>
{
    public async Task<UnitResult<Error>> Handle(
        DeleteItemCommand command,
        CancellationToken cancellationToken = new ())
    {
        Item? item = await repository.GetByIdAsync(command.ItemId, cancellationToken);
        if (item is null)
        {
            return GeneralErrors.NotFound(command.ItemId, "item");
        }

        Guid userId = currentUser.RequireId();

        // Удалять может владелец или модератор/админ (permission items.manage).
        if (!item.IsOwnedBy(userId) && !currentUser.HasPermission(TemplatePermissions.ITEMS_MANAGE))
        {
            return GeneralErrors.Forbidden();
        }

        repository.Remove(item);
        await repository.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}
