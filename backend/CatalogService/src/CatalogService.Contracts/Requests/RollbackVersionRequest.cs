namespace CatalogService.Contracts.Requests;

public sealed record RollbackVersionRequest(long TargetNumber, string? Note);
