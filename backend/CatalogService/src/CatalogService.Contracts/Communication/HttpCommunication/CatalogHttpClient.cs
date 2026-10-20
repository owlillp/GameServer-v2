using System.Net.Http.Headers;
using System.Net.Http.Json;
using CSharpFunctionalExtensions;
using CatalogService.Contracts.Dtos;
using Microsoft.Extensions.Logging;
using Shared.Communication;
using Shared.SharedKernel.Errors;

namespace CatalogService.Contracts.Communication.HttpCommunication;

public sealed class CatalogHttpClient(
    HttpClient httpClient,
    IServiceTokenProvider tokenProvider,
    ILogger<CatalogHttpClient> logger) : ICatalogClient
{
    public async Task<Result<CatalogItemSnapshot, Error>> GetItemAsync(
        Guid itemId,
        CancellationToken cancellationToken)
    {
        try
        {
            string token = await tokenProvider.GetAccessTokenAsync(cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"/catalog/items/{itemId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Catalog item {ItemId} request failed with status {StatusCode}",
                    itemId,
                    response.StatusCode);

                return GeneralErrors.Failure("Catalog item is unavailable");
            }

            CatalogEnvelope? envelope = await response.Content
                .ReadFromJsonAsync<CatalogEnvelope>(cancellationToken);

            if (envelope is null || envelope.IsError || envelope.Result is null)
            {
                return GeneralErrors.Failure("Catalog item is unavailable");
            }

            ItemDetailDto item = envelope.Result;
            return new CatalogItemSnapshot(item.Id, item.Code, item.Kind, item.Slot, item.DisplayName);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to load catalog item {ItemId}", itemId);
            return GeneralErrors.Failure("Catalog is unavailable");
        }
    }

    private sealed record CatalogEnvelope(ItemDetailDto? Result, bool IsError);
}

