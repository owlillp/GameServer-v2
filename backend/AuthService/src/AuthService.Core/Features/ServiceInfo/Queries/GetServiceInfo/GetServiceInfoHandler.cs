using AuthService.Contracts.Auth;
using CSharpFunctionalExtensions;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace AuthService.Core.Features.ServiceInfo.Queries.GetServiceInfo;

public sealed class GetServiceInfoHandler : IQueryHandler<ServiceInfoDto, GetServiceInfoQuery>
{
    public Task<Result<ServiceInfoDto, Error>> Handle(
        GetServiceInfoQuery query,
        CancellationToken cancellationToken = new ()) =>
        Task.FromResult<Result<ServiceInfoDto, Error>>(
            new ServiceInfoDto("AuthService", "v1", "stub"));
}
