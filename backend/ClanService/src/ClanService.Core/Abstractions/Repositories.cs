using ClanService.Domain.Clans;
using ClanService.Domain.Members;
using ClanService.Domain.Points;
using ClanService.Domain.Requests;

namespace ClanService.Core.Abstractions;

public interface IClanRepository
{
    Task<Clan?> GetAsync(Guid clanId, CancellationToken cancellationToken);

    Task<bool> TagExistsAsync(string tag, CancellationToken cancellationToken);

    Task<IReadOnlyList<Clan>> SearchAsync(string? query, bool activeOnly, int limit, CancellationToken cancellationToken);

    Task AddAsync(Clan clan, CancellationToken cancellationToken);

    /// <summary>
    /// <c>UPDATE ... SET member_count = member_count + 1 WHERE member_count &lt; @max</c>:
    /// false when the clan is full or disbanded. Runs in the current transaction.
    /// </summary>
    Task<bool> TryReserveSlotAsync(Guid clanId, int maxMembers, CancellationToken cancellationToken);

    Task ReleaseSlotAsync(Guid clanId, CancellationToken cancellationToken);

    /// <summary>
    /// <c>clan_points = clan_points + @amount</c> in the current transaction.
    /// </summary>
    Task AddPointsAsync(Guid clanId, long amount, CancellationToken cancellationToken);

    /// <summary>
    /// Clans whose creation fee hold is not committed yet, locked with <c>FOR UPDATE SKIP LOCKED</c>.
    /// </summary>
    Task<IReadOnlyList<Clan>> ClaimFeePendingAsync(int limit, CancellationToken cancellationToken);
}

public interface IClanMemberRepository
{
    Task<ClanMember?> GetAsync(Guid accountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ClanMember>> ListByClanAsync(Guid clanId, CancellationToken cancellationToken);

    Task AddAsync(ClanMember member, CancellationToken cancellationToken);

    void Remove(ClanMember member);

    /// <summary>
    /// Last time the player left a clan on their own (the cooldown start), if any.
    /// </summary>
    Task<DateTime?> GetLastVoluntaryLeaveAsync(Guid accountId, CancellationToken cancellationToken);

    Task AddHistoryAsync(ClanMemberHistory history, CancellationToken cancellationToken);
}

public interface IClanRequestRepository
{
    Task<ClanJoinRequest?> GetAsync(Guid requestId, CancellationToken cancellationToken);

    Task<bool> HasPendingAsync(Guid clanId, Guid accountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ClanJoinRequest>> ListPendingByClanAsync(Guid clanId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ClanJoinRequest>> ListPendingByAccountAsync(Guid accountId, CancellationToken cancellationToken);

    Task AddAsync(ClanJoinRequest request, CancellationToken cancellationToken);
}

public interface IClanPointsRepository
{
    Task AddAsync(ClanPointsEntry entry, CancellationToken cancellationToken);

    Task<IReadOnlyList<ClanPointsEntry>> ListByClanAsync(Guid clanId, int limit, CancellationToken cancellationToken);
}
