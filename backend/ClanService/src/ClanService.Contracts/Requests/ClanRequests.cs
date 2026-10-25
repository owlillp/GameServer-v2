namespace ClanService.Contracts.Requests;

public sealed record CreateClanRequest(string Tag, string Name, string? Description);

public sealed record UpdateClanRequest(string Name, string? Description);

public sealed record InvitePlayerRequest(Guid AccountId);

public sealed record ChangeRoleRequest(string Role);

public sealed record TransferLeadershipRequest(Guid AccountId);

public sealed record AdjustPointsRequest(long Amount, string Reason, string? Comment, Guid? OperationId);
