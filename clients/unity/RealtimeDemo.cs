using System;
using System.Text.Json;
using System.Threading.Tasks;
using GameServer.Realtime;
using UnityEngine;

public sealed class RealtimeDemo : MonoBehaviour
{
    [SerializeField] private string apiBaseUrl = "https://localhost:6101";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private RealtimeClient? _client;

    private async void Start()
    {
        _client = new RealtimeClient(apiBaseUrl, GetAccessTokenAsync);

        await RefreshSnapshotAsync();

        try
        {
            await _client.ConnectAsync();
        }
        catch (Exception exception)
        {
            Debug.LogError($"Realtime connection failed: {exception.Message}");
        }
    }

    private void Update()
    {
        if (_client is null)
        {
            return;
        }

        while (_client.TryDequeueNotification(out RealtimeNotification notification))
        {
            Handle(notification);
        }

        while (_client.TryDequeueAuthenticatedAccount(out Guid accountId))
        {
            Debug.Log($"[Realtime] authenticated as {accountId}");
        }
    }

    private void Handle(RealtimeNotification notification)
    {
        switch (notification.Type)
        {
            case "event.started":
            case "event.ended":
                ShowBanner(
                    notification.Type == "event.started"
                        ? "Ивент начался"
                        : "Ивент завершился");

                _ = RefreshSnapshotAsync();
                break;

            case "wallet.credited":
            case "wallet.debited":
            {
                WalletPayload payload = notification.Payload.Deserialize<WalletPayload>(JsonOptions)!;
                string sign = notification.Type == "wallet.credited" ? "+" : "-";

                ShowBanner($"{payload.Currency} {sign}{payload.Amount}");

                _ = RefreshWalletAsync();
                break;
            }

            case "inventory.items.granted":
            case "event.reward.granted":
                ShowBanner("Получены новые предметы");
                _ = RefreshInventoryAsync();
                break;
        }
    }

    private Task RefreshSnapshotAsync()
    {
        Debug.Log("[Realtime] refresh snapshot: /events, /wallets/me, /inventory/me");
        return Task.CompletedTask;
    }

    private Task RefreshWalletAsync()
    {
        Debug.Log("[Realtime] refresh /wallets/me");
        return Task.CompletedTask;
    }

    private Task RefreshInventoryAsync()
    {
        Debug.Log("[Realtime] refresh /inventory/me");
        return Task.CompletedTask;
    }

    private void ShowBanner(string text) =>
        Debug.Log($"[Realtime] {text}");

    private Task<string> GetAccessTokenAsync() =>
        Task.FromResult(PlayerPrefs.GetString("access_token"));

    private void OnDestroy()
    {
        if (_client is not null)
        {
            _ = _client.DisposeAsync();
        }
    }

    private sealed class WalletPayload
    {
        public Guid AccountId { get; set; }
        public Guid WalletId { get; set; }
        public string Currency { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public Guid OperationId { get; set; }
    }
}
