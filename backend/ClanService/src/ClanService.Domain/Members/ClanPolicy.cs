namespace ClanService.Domain.Members;

/// <summary>
/// What each clan role may do. Leader > Officer > Member; nobody acts on an equal or higher role
/// except the leader, who is unique and changes only by transfer.
/// </summary>
public static class ClanPolicy
{
    public static bool CanManageRequests(ClanRole actor) => actor is ClanRole.Leader or ClanRole.Officer;

    public static bool CanKick(ClanRole actor, ClanRole target) =>
        target != ClanRole.Leader && actor > target;

    public static bool CanChangeRole(ClanRole actor, ClanRole target, ClanRole newRole) =>
        actor == ClanRole.Leader && target != ClanRole.Leader && newRole != ClanRole.Leader;

    public static bool CanTransferLeadership(ClanRole actor) => actor == ClanRole.Leader;

    public static bool CanDisband(ClanRole actor) => actor == ClanRole.Leader;

    public static bool CanEditInfo(ClanRole actor) => actor is ClanRole.Leader or ClanRole.Officer;
}
