using CSharpFunctionalExtensions;
using EventService.Domain.Events;
using Shared.SharedKernel.Errors;

namespace EventService.Core.Abstractions;

public interface IEventRepository
{
    Task<Result<GameEvent, Error>> GetByIdAsync(
        Guid eventId,
        bool includeDetails,
        CancellationToken cancellationToken);

    Task<Result<GameEvent, Error>> GetByCodeAsync(
        string code,
        bool includeDetails,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<GameEvent>> ListAsync(
        bool includeDetails,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<GameEvent>> ListActiveAsync(DateTime now, CancellationToken cancellationToken);

    Task<IReadOnlyList<GameEvent>> ListPendingNotificationsAsync(
        DateTime now,
        CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(string code, Guid? excludeEventId, CancellationToken cancellationToken);

    Task AddAsync(GameEvent gameEvent, CancellationToken cancellationToken);
}
