using TemplateService.Domain;

namespace TemplateService.Core.Abstractions;

public interface IReadDbContext
{
    IQueryable<Item> ItemsRead { get; }
}
