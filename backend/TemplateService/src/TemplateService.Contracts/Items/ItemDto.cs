namespace TemplateService.Contracts.Items;

public sealed record ItemDto(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
