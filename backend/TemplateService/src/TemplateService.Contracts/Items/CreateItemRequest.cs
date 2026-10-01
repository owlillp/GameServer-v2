namespace TemplateService.Contracts.Items;

public sealed record CreateItemRequest(
    string Name,
    string? Description);
