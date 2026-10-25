using ClanService.Contracts.Dtos;
using ClanService.Core.Abstractions;
using ClanService.Core.Features.Clans.Models;
using ClanService.Core.Services;
using ClanService.Domain;
using ClanService.Domain.Clans;
using ClanService.Domain.Requests;
using CSharpFunctionalExtensions;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace ClanService.Core.Features.Clans.Commands;

public sealed record RequestJoinCommand(Guid AccountId, Guid ClanId) : ICommand;

public sealed class RequestJoinHandler(
    IClanRepository clanRepository,
    IClanRequestRepository requestRepository,
    ClanMembership membership,
    ITransactionManager transactionManager) : ICommandHandler<ClanJoinRequestDto, RequestJoinCommand>
{
    public async Task<Result<ClanJoinRequestDto, Error>> Handle(RequestJoinCommand command, CancellationToken cancellationToken = default)
    {
        Clan? clan = await clanRepository.GetAsync(command.ClanId, cancellationToken);
        if (clan is null)
        {
            return ClanErrors.ClanNotFound;
        }

        if (!clan.IsActive)
        {
            return ClanErrors.ClanDisbanded;
        }

        var canJoin = await membership.EnsureCanJoinAsync(command.AccountId, cancellationToken);
        if (canJoin.IsFailure)
        {
            return canJoin.Error;
        }

        if (await requestRepository.HasPendingAsync(clan.Id, command.AccountId, cancellationToken))
        {
            return ClanErrors.RequestDuplicate;
        }

        var request = new ClanJoinRequest(clan.Id, command.AccountId, ClanRequestKind.Request, command.AccountId, membership.Now);
        await requestRepository.AddAsync(request, cancellationToken);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? ClanSaveErrors.Map(saveResult.Error) : request.ToDto(clan.Tag);
    }
}
