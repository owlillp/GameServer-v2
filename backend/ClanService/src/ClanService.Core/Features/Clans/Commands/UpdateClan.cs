using ClanService.Contracts.Dtos;
using ClanService.Core.Abstractions;
using ClanService.Core.Features.Clans.Models;
using ClanService.Core.Services;
using ClanService.Domain;
using ClanService.Domain.Clans;
using ClanService.Domain.Members;
using CSharpFunctionalExtensions;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace ClanService.Core.Features.Clans.Commands;

public sealed record UpdateClanCommand(Guid AccountId, Guid ClanId, string Name, string? Description) : ICommand;

public sealed class UpdateClanHandler(
    IClanRepository clanRepository,
    ClanMembership membership,
    ITransactionManager transactionManager) : ICommandHandler<ClanDto, UpdateClanCommand>
{
    public async Task<Result<ClanDto, Error>> Handle(UpdateClanCommand command, CancellationToken cancellationToken = default)
    {
        var actorResult = await membership.GetActorAsync(command.ClanId, command.AccountId, cancellationToken);
        if (actorResult.IsFailure)
        {
            return actorResult.Error;
        }

        if (!ClanPolicy.CanEditInfo(actorResult.Value.Role))
        {
            return ClanErrors.Forbidden;
        }

        Clan? clan = await clanRepository.GetAsync(command.ClanId, cancellationToken);
        if (clan is null)
        {
            return ClanErrors.ClanNotFound;
        }

        var updateResult = clan.UpdateInfo(command.Name, command.Description);
        if (updateResult.IsFailure)
        {
            return updateResult.Error;
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? saveResult.Error : clan.ToDto(membership.MaxMembers);
    }
}
