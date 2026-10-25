using ClanService.Contracts.Dtos;
using ClanService.Core.Abstractions;
using ClanService.Core.Features.Clans.Models;
using ClanService.Core.Services;
using CSharpFunctionalExtensions;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace ClanService.Core.Features.Admin;

public sealed record AdjustPointsCommand(Guid AdminAccountId, Guid ClanId, long Amount, string Reason, string? Comment, Guid? OperationId) : ICommand;

public sealed record GetPointsLedgerQuery(Guid ClanId, int Limit) : IQuery;

/// <summary>
/// Manual correction of clan points; the ledger entry keeps who did it and why (audit).
/// </summary>
public sealed class AdjustPointsHandler(ClanPointsService pointsService) : ICommandHandler<ClanPointsEntryDto, AdjustPointsCommand>
{
    private const string REASON_PREFIX = "admin:";

    public async Task<Result<ClanPointsEntryDto, Error>> Handle(AdjustPointsCommand command, CancellationToken cancellationToken = default)
    {
        var result = await pointsService.ApplyAsync(
            command.ClanId,
            command.OperationId ?? Guid.CreateVersion7(),
            command.Amount,
            string.IsNullOrWhiteSpace(command.Reason) ? string.Empty : REASON_PREFIX + command.Reason.Trim(),
            command.AdminAccountId,
            command.Comment,
            cancellationToken);

        return result.IsFailure ? result.Error : result.Value.ToDto();
    }
}

public sealed class GetPointsLedgerHandler(IClanPointsRepository pointsRepository)
    : IQueryHandler<IReadOnlyList<ClanPointsEntryDto>, GetPointsLedgerQuery>
{
    public async Task<Result<IReadOnlyList<ClanPointsEntryDto>, Error>> Handle(
        GetPointsLedgerQuery query,
        CancellationToken cancellationToken = default)
    {
        var entries = await pointsRepository.ListByClanAsync(query.ClanId, Math.Clamp(query.Limit, 1, 500), cancellationToken);
        return entries.Select(entry => entry.ToDto()).ToList();
    }
}
