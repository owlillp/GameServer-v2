using CSharpFunctionalExtensions;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Shared.Communication;
using Shared.Grpc.Session;
using Shared.SharedKernel.Errors;

namespace SessionService.Contracts.Communication;

public sealed class SessionTicketGrpcClient(
    MatchSessions.MatchSessionsClient client,
    IServiceTokenProvider tokenProvider,
    IOptions<SessionServiceOptions> options) : ISessionTicketClient
{
    private static readonly TimeSpan RETRY_DELAY = TimeSpan.FromMilliseconds(200);

    public async Task<Result<MatchTicket, Error>> IssueMatchTicketAsync(
        Guid matchId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var request = new IssueMatchTicketRequest
        {
            MatchId = matchId.ToString(),
            AccountId = accountId.ToString(),
        };

        try
        {
            IssueMatchTicketReply reply = await InvokeAsync(request, cancellationToken);
            if (!reply.Success)
            {
                return Error.NotFound(reply.ErrorCode, reply.ErrorMessage);
            }

            return new MatchTicket(
                Guid.Parse(reply.SessionId),
                reply.ServerAddress,
                reply.Ticket,
                reply.ExpiresAt.ToDateTime());
        }
        catch (RpcException exception)
        {
            return GeneralErrors.Failure($"Session service is unavailable: {exception.Status.Detail}").AsTransient();
        }
    }

    private async Task<IssueMatchTicketReply> InvokeAsync(
        IssueMatchTicketRequest request,
        CancellationToken cancellationToken)
    {
        SessionServiceOptions current = options.Value;
        bool tokenRefreshed = false;
        int attempt = 0;

        while (true)
        {
            attempt++;
            string token = await tokenProvider.GetAccessTokenAsync(cancellationToken);
            var callOptions = new CallOptions(
                headers: new Metadata { { "authorization", $"Bearer {token}" } },
                deadline: DateTime.UtcNow.AddSeconds(current.DeadlineSeconds),
                cancellationToken: cancellationToken);

            try
            {
                return await client.IssueMatchTicketAsync(request, callOptions).ResponseAsync;
            }
            catch (RpcException exception)
                when (exception.StatusCode is StatusCode.Unauthenticated or StatusCode.PermissionDenied && !tokenRefreshed)
            {
                tokenProvider.Invalidate(token);
                tokenRefreshed = true;
            }
            catch (RpcException exception)
                when (attempt < current.MaxAttempts
                    && exception.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
            {
                await Task.Delay(RETRY_DELAY, cancellationToken);
            }
        }
    }
}
