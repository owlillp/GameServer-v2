using ClanService.Contracts.Dtos;
using ClanService.Core.Abstractions;
using ClanService.Core.Configuration;
using ClanService.Core.Features.Clans.Models;
using ClanService.Core.Services;
using ClanService.Domain;
using ClanService.Domain.Clans;
using ClanService.Domain.Members;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;
using WalletService.Contracts.Communication;

namespace ClanService.Core.Features.Clans.Commands;

public sealed record CreateClanCommand(Guid AccountId, string Tag, string Name, string? Description) : ICommand;

/// <summary>
/// Creates a clan with the player as leader. With a creation fee: reserve in Wallet → create →
/// commit the hold; if creation fails the hold is released, if the commit fails the fee sweeper retries.
/// </summary>
public sealed class CreateClanHandler(
    IClanRepository clanRepository,
    IClanMemberRepository memberRepository,
    IClanEventPublisher eventPublisher,
    ClanMembership membership,
    IWalletClient walletClient,
    ITransactionManager transactionManager,
    IOptionsMonitor<ClanOptions> options,
    ILogger<CreateClanHandler> logger) : ICommandHandler<MyClanDto, CreateClanCommand>
{
    public async Task<Result<MyClanDto, Error>> Handle(CreateClanCommand command, CancellationToken cancellationToken = default)
    {
        var tagResult = Clan.NormalizeTag(command.Tag);
        if (tagResult.IsFailure)
        {
            return tagResult.Error;
        }

        if (await clanRepository.TagExistsAsync(tagResult.Value, cancellationToken))
        {
            return ClanErrors.TagTaken;
        }

        var canJoin = await membership.EnsureCanJoinAsync(command.AccountId, cancellationToken);
        if (canJoin.IsFailure)
        {
            return canJoin.Error;
        }

        Guid clanId = Guid.CreateVersion7();
        ClanOptions current = options.CurrentValue;
        Guid? holdId = null;

        if (current.CreationFee > 0)
        {
            var reserveResult = await walletClient.ReserveAsync(
                command.AccountId,
                current.CreationFeeCurrency,
                current.CreationFee,
                clanId,
                $"clan:{clanId}",
                cancellationToken);

            if (reserveResult.IsFailure)
            {
                return ClanErrors.CreationFeeFailed;
            }

            holdId = Guid.Parse(reserveResult.Value);
        }

        var createResult = await CreateAsync(command, clanId, holdId, cancellationToken);
        if (createResult.IsFailure)
        {
            if (holdId is not null)
            {
                await walletClient.ReleaseAsync(holdId.Value, cancellationToken);
            }

            return createResult.Error;
        }

        (Clan clan, ClanMember leader) = createResult.Value;

        if (holdId is not null)
        {
            await CommitFeeAsync(clan, holdId.Value, cancellationToken);
        }

        return new MyClanDto(clan.ToDto(current.MaxMembers), leader.ToDto().Role, [leader.ToDto()]);
    }

    private async Task<Result<(Clan Clan, ClanMember Leader), Error>> CreateAsync(
        CreateClanCommand command,
        Guid clanId,
        Guid? holdId,
        CancellationToken cancellationToken)
    {
        var clanResult = Clan.Create(clanId, command.Tag, command.Name, command.Description, command.AccountId, holdId, membership.Now);
        if (clanResult.IsFailure)
        {
            return clanResult.Error;
        }

        Clan clan = clanResult.Value;
        var leader = new ClanMember(clan.Id, command.AccountId, ClanRole.Leader, clan.CreatedAt);

        await clanRepository.AddAsync(clan, cancellationToken);
        await memberRepository.AddAsync(leader, cancellationToken);
        await eventPublisher.PublishClanCreatedAsync(clan);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? ClanSaveErrors.Map(saveResult.Error) : (clan, leader);
    }

    private async Task CommitFeeAsync(Clan clan, Guid holdId, CancellationToken cancellationToken)
    {
        var commitResult = await walletClient.CommitAsync(holdId, cancellationToken);
        if (commitResult.IsFailure)
        {
            logger.LogWarning("Creation fee of clan {ClanId} not committed yet: {Error}", clan.Id, commitResult.Error.GetMessage());
            return;
        }

        clan.MarkFeeCommitted(membership.Now);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            logger.LogWarning("Fee commit of clan {ClanId} not saved, the sweeper will retry", clan.Id);
        }
    }
}
