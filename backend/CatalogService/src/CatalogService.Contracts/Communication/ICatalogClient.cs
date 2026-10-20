using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace CatalogService.Contracts.Communication;

public sealed record CatalogItemSnapshot(
    Guid Id,
    string Code,
    string Kind,
    string? Slot,
    string? DisplayName);

public interface ICatalogClient
{
    Task<Result<CatalogItemSnapshot, Error>> GetItemAsync(Guid itemId, CancellationToken cancellationToken);
}
