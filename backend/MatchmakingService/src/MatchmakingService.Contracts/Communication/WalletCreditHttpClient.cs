using System.Net.Http.Headers;
using System.Net.Http.Json;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Shared.Communication;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Contracts.Communication;

public sealed class WalletCreditHttpClient(
    HttpClient httpClient,
    IServiceTokenProvider tokenProvider,
    ILogger<WalletCreditHttpClient> logger) : IWalletCreditClient
{
    public async Task<Result<bool, Error>> CreditAsync(
        Guid accountId,
        string currency,
        long amount,
        Guid operationId,
        string? referenceId,
        CancellationToken cancellationToken)
    {
        try
        {
            string token = await tokenProvider.GetAccessTokenAsync(cancellationToken);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/wallets/{accountId}/credit")
            {
                Content = JsonContent.Create(new
                {
                    currency,
                    amount,
                    reason = "MatchReward",
                    operationId,
                    referenceId,
                }),
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Wallet credit for account {AccountId} failed with status {StatusCode}",
                    accountId,
                    response.StatusCode);

                return GeneralErrors.Failure($"Wallet credit failed with status {(int)response.StatusCode}");
            }

            WalletEnvelope? envelope = await response.Content
                .ReadFromJsonAsync<WalletEnvelope>(cancellationToken);

            if (envelope is null || envelope.IsError)
            {
                return GeneralErrors.Failure("Wallet credit failed");
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to credit wallet for account {AccountId}", accountId);
            return GeneralErrors.Failure("Wallet is unavailable");
        }
    }

    private sealed record WalletEnvelope(bool IsError);
}
