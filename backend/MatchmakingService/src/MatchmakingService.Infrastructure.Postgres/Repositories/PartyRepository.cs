using MatchmakingService.Core.Abstractions;
using MatchmakingService.Domain.Parties;
using Microsoft.EntityFrameworkCore;

namespace MatchmakingService.Infrastructure.Postgres.Repositories;

public sealed class PartyRepository(MatchmakingServiceDbContext dbContext) : IPartyRepository
{
    public Task<Party?> GetAsync(Guid partyId, CancellationToken cancellationToken) =>
        dbContext.Parties
            .Include(party => party.Members)
            .FirstOrDefaultAsync(party => party.Id == partyId, cancellationToken);

    public Task<Party?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        string normalized = code.Trim().ToUpperInvariant();

        return dbContext.Parties
            .Include(party => party.Members)
            .FirstOrDefaultAsync(party => party.Code == normalized, cancellationToken);
    }

    public Task<Party?> GetByMemberAsync(Guid accountId, CancellationToken cancellationToken) =>
        dbContext.Parties
            .Include(party => party.Members)
            .Where(party => party.Status != PartyStatus.Closed)
            .Where(party => party.Members.Any(member => member.AccountId == accountId))
            .OrderByDescending(party => party.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Party>> ListAsync(
        IReadOnlyCollection<Guid> partyIds,
        CancellationToken cancellationToken)
    {
        Guid[] ids = [.. partyIds];

        return await dbContext.Parties
            .Include(party => party.Members)
            .Where(party => ids.Contains(party.Id))
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Party party, CancellationToken cancellationToken) =>
        dbContext.Parties.AddAsync(party, cancellationToken).AsTask();
}
