# Unity Realtime client

## Установка

1. Добавь через NuGetForUnity пакет `Microsoft.AspNetCore.SignalR.Client`.
2. Скопируй в проект `RealtimeClient.cs` и `RealtimeDemo.cs`.
3. Создай на сцене пустой GameObject и повесь на него `RealtimeDemo`.
4. В инспекторе укажи `Api Base Url` (например, `https://localhost:6101`).
5. В `RealtimeDemo.GetAccessTokenAsync` подставь токен из своего auth-хранилища.

## Как это выглядит в игре

```csharp
private async void Start()
{
    _client = new RealtimeClient(apiBaseUrl, GetAccessTokenAsync);
    await RefreshSnapshotAsync();   // REST-снапшот
    await _client.ConnectAsync();   // WebSocket + Authenticate
}

private void Update()
{
    while (_client.TryDequeueNotification(out RealtimeNotification notification))
    {
        Handle(notification);
    }
}
```

`Handle` уже разбирает:

- `event.started` / `event.ended` → баннер и обновление снапшота;
- `wallet.credited` / `wallet.debited` → показать сумму и обновить `/wallets/me`;
- `inventory.items.granted` / `event.reward.granted` → обновить `/inventory/me`.

## Важно про Unity

- Колбэки SignalR приходят **не в main thread**. Клиент складывает уведомления в
  `ConcurrentQueue`, а `RealtimeDemo.Update()` разбирает их на главном потоке.
  Не вызывай `UnityEngine`-API прямо из колбэка SignalR.
- `UnityWebRequest` для получения тикета выполняется через
  `SynchronizationContext` главного потока.
- После `Reconnected` клиент автоматически берёт новый тикет и снова вызывает
  `Authenticate`; старый тикет одноразовый.
- `WithAutomaticReconnect(new InfiniteRetryPolicy())` переподключается бесконечно
  с backoff 0/2/10/30 секунд. После восстановления связи обязательно заново
  запроси снапшот — часть уведомлений могла быть пропущена.
- Дедупликация уже встроена по `notification.Id`.
- Для WebGL: `UnityWebRequest` и WebSocket работают, но нужен HTTPS/WSS и
  корректный сертификат; для self-signed в dev либо доверь сертификат, либо
  используй HTTP-стенд.
