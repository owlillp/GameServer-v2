using CatalogService.Contracts.Requests;
using Shared.Core.Abstractions;

namespace CatalogService.Core.Features.Versions.Commands.PublishVersion;

public sealed record PublishVersionCommand(PublishVersionRequest Request, Guid? PublishedBy) : ICommand;
