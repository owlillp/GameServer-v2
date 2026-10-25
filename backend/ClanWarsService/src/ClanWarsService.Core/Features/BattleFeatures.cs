using ClanWarsService.Contracts;
using ClanWarsService.Core.Abstractions;
using ClanWarsService.Core.Services;
using ClanWarsService.Domain.Seasons;
using CSharpFunctionalExtensions;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace ClanWarsService.Core.Features;

public sealed record QueueSquadCommand(Guid AccountId, IReadOnlyList<Guid> AccountIds) : ICommand;

public sealed record LeaveBattleQueueCommand(Guid AccountId) : ICommand;

public sealed record GetMyBattlesQuery(Guid AccountId) : IQuery;

public sealed record ListBattleWindowsQuery(Guid SeasonId) : IQuery;

public sealed class QueueSquadHandler(BattleQueueService queueService) : ICommandHandler<BattleQueueEntryDto, QueueSquadCommand>
{
    public async Task<Result<BattleQueueEntryDto, Error>> Handle(QueueSquadCommand command, CancellationToken cancellationToken = default)
    {
        var result = await queueService.QueueAsync(command.AccountId, command.AccountIds, cancellationToken);
        return result.IsFailure ? result.Error : result.Value.ToDto();
    }
}

public sealed class LeaveBattleQueueHandler(BattleQueueService queueService) : ICommandHandler<bool, LeaveBattleQueueCommand>
{
    public async Task<Result<bool, Error>> Handle(LeaveBattleQueueCommand command, CancellationToken cancellationToken = default)
    {
        var result = await queueService.LeaveAsync(command.AccountId, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error;
        }

        return true;
    }
}

public sealed class GetMyBattlesHandler(BattleQueueService queueService) : IQueryHandler<MyBattlesDto, GetMyBattlesQuery>
{
    public Task<Result<MyBattlesDto, Error>> Handle(GetMyBattlesQuery query, CancellationToken cancellationToken = default) =>
        queueService.GetMineAsync(query.AccountId, cancellationToken);
}

public sealed class ListBattleWindowsHandler(
    IBattleRepository battleRepository,
    TimeProvider timeProvider) : IQueryHandler<IReadOnlyList<BattleWindowDto>, ListBattleWindowsQuery>
{
    public async Task<Result<IReadOnlyList<BattleWindowDto>, Error>> Handle(ListBattleWindowsQuery query, CancellationToken cancellationToken = default)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        IReadOnlyList<BattleWindow> windows = await battleRepository.ListWindowsAsync(query.SeasonId, cancellationToken);

        return windows.Select(window => window.ToDto(now)).ToList();
    }
}
