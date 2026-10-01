namespace TemplateService.Contracts.Items;

public sealed record UpdateItemRequest(
    string Name,
    string? Description);
