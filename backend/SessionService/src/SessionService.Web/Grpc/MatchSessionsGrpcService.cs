using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using SessionService.Core.Matches;
using Shared.Grpc.Session;
using Shared.SharedKernel.Errors;

namespace SessionService.Web.Grpc;

public sealed class MatchSessionsGrpcService(MatchTicketIssuer issuer) : MatchSessions.MatchSessionsBase
{
    public override async Task<IssueMatchTicketReply> IssueMatchTicket(
        IssueMatchTicketRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.MatchId, out Guid matchId) || !Guid.TryParse(request.AccountId, out Guid accountId))
        {
            return new IssueMatchTicketReply
            {
                Success = false,
                ErrorCode = "session.invalid_request",
                ErrorMessage = "match_id and account_id must be GUIDs",
            };
        }

        var result = await issuer.IssueAsync(matchId, accountId, context.CancellationToken);
        if (result.IsFailure)
        {
            if (result.Error.Behavior == ErrorBehavior.Transient || result.Error.Type == ErrorType.FAILURE)
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "Temporary failure, retry the call"));
            }

            return new IssueMatchTicketReply
            {
                Success = false,
                ErrorCode = result.Error.Messages.Count > 0 ? result.Error.Messages[0].Code : "session.failed",
                ErrorMessage = result.Error.GetMessage(),
            };
        }

        return new IssueMatchTicketReply
        {
            Success = true,
            SessionId = result.Value.SessionId.ToString(),
            ServerAddress = result.Value.ServerAddress,
            Ticket = result.Value.Ticket,
            ExpiresAt = Timestamp.FromDateTime(DateTime.SpecifyKind(result.Value.ExpiresAt, DateTimeKind.Utc)),
        };
    }
}
