using ClanService.Core.Abstractions;
using ClanService.Domain.Clans;
using ClanService.Domain.Members;
using ClanService.Domain.Points;
using ClanService.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace ClanService.Infrastructure.Postgres.Repositories;

public sealed class ClanRepository(ClanServiceDbContext dbContext) : IClanRepository
{
    public Task<Clan?> GetAsync(Guid clanId, CancellationToken cancellationToken) =>
        dbContext.Clans.FirstOrDefaultAsync(clan => clan.Id == clanId, cancellationToken);

    public Task<bool> TagExistsAsync(string tag, CancellationToken cancellationToken) =>
        dbContext.Clans.AnyAsync(clan => clan.Tag == tag, cancellationToken);

    public async Task<IReadOnlyList<Clan>> SearchAsync(
        string? query,
        bool activeOnly,
        int limit,
        CancellationToken cancellationToken)
    {
        IQueryable<Clan> clans = dbContext.Clans.AsNoTracking();

        if (activeOnly)
        {
            clans = clans.Where(clan => clan.Status == ClanStatus.Active);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            string trimmed = query.Trim();
            string tagPrefix = trimmed.ToUpperInvariant() + "%";
            string namePattern = "%" + trimmed + "%";

            clans = clans.Where(clan =>
                EF.Functions.Like(clan.Tag, tagPrefix) || EF.Functions.ILike(clan.Name, namePattern));
        }

        return await clans
            .OrderByDescending(clan => clan.ClanPoints)
            .ThenBy(clan => clan.Tag)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Clan clan, CancellationToken cancellationToken) =>
        dbContext.Clans.AddAsync(clan, cancellationToken).AsTask();

    public async Task<bool> TryReserveSlotAsync(Guid clanId, int maxMembers, CancellationToken cancellationToken)
    {
        string active = nameof(ClanStatus.Active);

        int updated = await dbContext.Database.ExecuteSqlAsync(
            $"""
            UPDATE clans.clans SET member_count = member_count + 1
            WHERE id = {clanId} AND status = {active} AND member_count < {maxMembers}
            """,
            cancellationToken);

        return updated == 1;
    }

    public Task ReleaseSlotAsync(Guid clanId, CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlAsync(
            $"UPDATE clans.clans SET member_count = GREATEST(member_count - 1, 0) WHERE id = {clanId}",
            cancellationToken);

    public Task AddPointsAsync(Guid clanId, long amount, CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlAsync(
            $"UPDATE clans.clans SET clan_points = clan_points + {amount} WHERE id = {clanId}",
            cancellationToken);

    public async Task<IReadOnlyList<Clan>> ClaimFeePendingAsync(int limit, CancellationToken cancellationToken)
    {
        List<Guid> ids = await dbContext.Database
            .SqlQuery<Guid>($"""
                SELECT id AS "Value" FROM clans.clans
                WHERE fee_hold_id IS NOT NULL AND fee_committed_at IS NULL
                ORDER BY created_at
                LIMIT {limit}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        if (ids.Count == 0)
        {
            return [];
        }

        return await dbContext.Clans.Where(clan => ids.Contains(clan.Id)).ToListAsync(cancellationToken);
    }
}

public sealed class ClanMemberRepository(ClanServiceDbContext dbContext) : IClanMemberRepository
{
    public Task<ClanMember?> GetAsync(Guid accountId, CancellationToken cancellationToken) =>
        dbContext.Members.FirstOrDefaultAsync(member => member.AccountId == accountId, cancellationToken);

    public async Task<IReadOnlyList<ClanMember>> ListByClanAsync(Guid clanId, CancellationToken cancellationToken) =>
        await dbContext.Members.Where(member => member.ClanId == clanId).ToListAsync(cancellationToken);

    public Task AddAsync(ClanMember member, CancellationToken cancellationToken) =>
        dbContext.Members.AddAsync(member, cancellationToken).AsTask();

    public void Remove(ClanMember member) => dbContext.Members.Remove(member);

    public Task<DateTime?> GetLastVoluntaryLeaveAsync(Guid accountId, CancellationToken cancellationToken) =>
        dbContext.MemberHistory
            .Where(history => history.AccountId == accountId && history.Reason == MemberLeaveReason.Left)
            .OrderByDescending(history => history.LeftAt)
            .Select(history => (DateTime?)history.LeftAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task AddHistoryAsync(ClanMemberHistory history, CancellationToken cancellationToken) =>
        dbContext.MemberHistory.AddAsync(history, cancellationToken).AsTask();
}

public sealed class ClanRequestRepository(ClanServiceDbContext dbContext) : IClanRequestRepository
{
    public Task<ClanJoinRequest?> GetAsync(Guid requestId, CancellationToken cancellationToken) =>
        dbContext.JoinRequests.FirstOrDefaultAsync(request => request.Id == requestId, cancellationToken);

    public Task<bool> HasPendingAsync(Guid clanId, Guid accountId, CancellationToken cancellationToken) =>
        dbContext.JoinRequests.AnyAsync(
            request => request.ClanId == clanId
                && request.AccountId == accountId
                && request.Status == ClanRequestStatus.Pending,
            cancellationToken);

    public async Task<IReadOnlyList<ClanJoinRequest>> ListPendingByClanAsync(Guid clanId, CancellationToken cancellationToken) =>
        await dbContext.JoinRequests
            .AsNoTracking()
            .Where(request => request.ClanId == clanId && request.Status == ClanRequestStatus.Pending)
            .OrderBy(request => request.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ClanJoinRequest>> ListPendingByAccountAsync(Guid accountId, CancellationToken cancellationToken) =>
        await dbContext.JoinRequests
            .AsNoTracking()
            .Where(request => request.AccountId == accountId && request.Status == ClanRequestStatus.Pending)
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task AddAsync(ClanJoinRequest request, CancellationToken cancellationToken) =>
        dbContext.JoinRequests.AddAsync(request, cancellationToken).AsTask();
}

public sealed class ClanPointsRepository(ClanServiceDbContext dbContext) : IClanPointsRepository
{
    public Task AddAsync(ClanPointsEntry entry, CancellationToken cancellationToken) =>
        dbContext.PointsLedger.AddAsync(entry, cancellationToken).AsTask();

    public async Task<IReadOnlyList<ClanPointsEntry>> ListByClanAsync(Guid clanId, int limit, CancellationToken cancellationToken) =>
        await dbContext.PointsLedger
            .AsNoTracking()
            .Where(entry => entry.ClanId == clanId)
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
}
