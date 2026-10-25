using ClanWarsService.Contracts;
using ClanWarsService.Core.Abstractions;
using ClanWarsService.Domain.Battles;
using CSharpFunctionalExtensions;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace ClanWarsService.Core.Features;

public sealed record ListAbuseSignalsQuery(Guid? SeasonId, int Limit) : IQuery;

public sealed class ListAbuseSignalsHandler(IBattleRepository battleRepository) : IQueryHandler<IReadOnlyList<AbuseSignalDto>, ListAbuseSignalsQuery>
{
    public async Task<Result<IReadOnlyList<AbuseSignalDto>, Error>> Handle(ListAbuseSignalsQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AbuseSignal> signals = await battleRepository.ListSignalsAsync(query.SeasonId, Math.Clamp(query.Limit, 1, 500), cancellationToken);
        return signals
            .Select(signal => new AbuseSignalDto(signal.Id, signal.Kind, signal.SeasonId, signal.ClanIds, signal.BattleId, signal.Details, signal.DetectedAt))
            .ToList();
    }
}
