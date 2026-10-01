using AuthService.Contracts.Auth;
using AuthService.Core.Features.ServiceInfo.Queries.GetServiceInfo;
using Microsoft.AspNetCore.Mvc;
using Shared.Core.Abstractions;
using Shared.Framework.Endpoints;

namespace AuthService.Web.Endpoints.ServiceInfo;

public sealed class ServiceInfoEndpoints : IEndpointGroup
{
    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup("/auth")
            .WithTags("Auth");

        group.MapGet("/service-info", GetServiceInfo)
            .WithName("Auth.ServiceInfo")
            .WithSummary("Информация о сервисе (заглушка AuthService)");

        return group;
    }

    private static async Task<EndpointResult<ServiceInfoDto>> GetServiceInfo(
        [FromServices] IQueryHandler<ServiceInfoDto, GetServiceInfoQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetServiceInfoQuery(), ct);
}
