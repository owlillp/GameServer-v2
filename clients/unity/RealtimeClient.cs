using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using UnityEngine;
using UnityEngine.Networking;

namespace GameServer.Realtime
{
    public sealed class RealtimeNotification
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
        public int Version { get; set; }
        public JsonElement Payload { get; set; }
    }

    public sealed class RealtimeClient : IAsyncDisposable
    {
        private readonly string _apiBaseUrl;
        private readonly Func<Task<string>> _accessTokenProvider;
        private readonly SynchronizationContext? _mainThread;
        private readonly ConcurrentQueue<RealtimeNotification> _notifications = new();
        private readonly ConcurrentQueue<Guid> _authenticated = new();
        private readonly HashSet<Guid> _seen = new();
        private readonly Queue<Guid> _seenOrder = new();
        private readonly object _seenGate = new();

        private const int SEEN_LIMIT = 2048;

        private HubConnection? _connection;
        private bool _disposed;

        public RealtimeClient(string apiBaseUrl, Func<Task<string>> accessTokenProvider)
        {
            _apiBaseUrl = apiBaseUrl.TrimEnd('/');
            _accessTokenProvider = accessTokenProvider;
            _mainThread = SynchronizationContext.Current;
        }

        public async Task ConnectAsync()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl($"{_apiBaseUrl}/realtime")
                .WithAutomaticReconnect(new InfiniteRetryPolicy())
                .Build();

            _connection.On<RealtimeNotification>("Notify", OnNotification);
            _connection.On<Guid>("Authenticated", accountId => _authenticated.Enqueue(accountId));
            _connection.Reconnected += _ => AuthenticateAsync();
            _connection.Closed += error =>
            {
                if (error is not null)
                {
                    Debug.LogWarning($"Realtime connection closed: {error.Message}");
                }

                return Task.CompletedTask;
            };

            await _connection.StartAsync();
            await AuthenticateAsync();
        }

        public bool TryDequeueNotification(out RealtimeNotification notification) =>
            _notifications.TryDequeue(out notification);

        public bool TryDequeueAuthenticatedAccount(out Guid accountId) =>
            _authenticated.TryDequeue(out accountId);

        private async Task AuthenticateAsync()
        {
            if (_connection is null || _disposed)
            {
                return;
            }

            string ticket = await RequestTicketAsync();
            await _connection.InvokeAsync("Authenticate", ticket);
        }

        private Task<string> RequestTicketAsync() =>
            RunOnMainThreadAsync(RequestTicketCoreAsync);

        private async Task<string> RequestTicketCoreAsync()
        {
            string accessToken = await _accessTokenProvider();

            using var request = new UnityWebRequest(
                $"{_apiBaseUrl}/realtime/ticket",
                UnityWebRequest.kHttpVerbPOST);

            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new InvalidOperationException(
                    $"Realtime ticket request failed: {request.responseCode} {request.error}");
            }

            TicketEnvelope envelope = JsonUtility.FromJson<TicketEnvelope>(
                request.downloadHandler.text);

            if (envelope?.result is null || string.IsNullOrEmpty(envelope.result.ticket))
            {
                throw new InvalidOperationException("Realtime ticket response is empty");
            }

            return envelope.result.ticket;
        }

        private void OnNotification(RealtimeNotification notification)
        {
            lock (_seenGate)
            {
                if (!_seen.Add(notification.Id))
                {
                    return;
                }

                _seenOrder.Enqueue(notification.Id);
                while (_seenOrder.Count > SEEN_LIMIT)
                {
                    _seen.Remove(_seenOrder.Dequeue());
                }
            }

            _notifications.Enqueue(notification);
        }

        private Task<T> RunOnMainThreadAsync<T>(Func<Task<T>> action)
        {
            if (_mainThread is null || SynchronizationContext.Current == _mainThread)
            {
                return action();
            }

            var completion = new TaskCompletionSource<T>();

            _mainThread.Post(
                async _ =>
                {
                    try
                    {
                        completion.SetResult(await action());
                    }
                    catch (Exception exception)
                    {
                        completion.SetException(exception);
                    }
                },
                null);

            return completion.Task;
        }

        public async ValueTask DisposeAsync()
        {
            _disposed = true;

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }
        }

        private sealed class InfiniteRetryPolicy : IRetryPolicy
        {
            public TimeSpan? NextRetryDelay(RetryContext retryContext) =>
                retryContext.PreviousRetryCount switch
                {
                    0 => TimeSpan.Zero,
                    <= 2 => TimeSpan.FromSeconds(2),
                    <= 5 => TimeSpan.FromSeconds(10),
                    _ => TimeSpan.FromSeconds(30),
                };
        }

        [Serializable]
        private sealed class TicketEnvelope
        {
            public TicketResult? result;
        }

        [Serializable]
        private sealed class TicketResult
        {
            public string ticket = string.Empty;
            public int expiresInSeconds;
        }
    }
}
