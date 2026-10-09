using CatalogService.Contracts.Requests;
using Shared.Core.Abstractions;

namespace CatalogService.Core.Features.Versions.Commands.RollbackVersion;

public sealed record RollbackVersionCommand(RollbackVersionRequest Request, Guid? PublishedBy) : ICommand;
