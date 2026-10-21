using MatchmakingService.Domain.Parties;

namespace MatchmakingService.Core.Abstractions;

public interface IPartyRepository
{
    Task<Party?> GetAsync(Guid partyId, CancellationToken cancellationToken);

    Task<Party?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<Party?> GetByMemberAsync(Guid accountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Party>> ListAsync(IReadOnlyCollection<Guid> partyIds, CancellationToken cancellationToken);

    Task AddAsync(Party party, CancellationToken cancellationToken);
}
