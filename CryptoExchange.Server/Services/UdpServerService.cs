using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using CryptoTrader.Shared.Models;

namespace CryptoExchange.Server.Services;

/// <summary>
/// UDP Server for broadcasting real-time price updates
/// </summary>
public class UdpServerService
{
    private readonly int _port;
    private UdpClient? _server;
    private CancellationTokenSource? _cts;
    private readonly ConcurrentDictionary<string, IPEndPoint> _subscribers = new();

    public event EventHandler<string>? OnLog;

    public UdpServerService(int port)
    {
        _port = port;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _server = new UdpClient(_port);

        Log($"UDP Server started on port {_port}");

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var result = await _server.ReceiveAsync(_cts.Token);
                var message = Encoding.UTF8.GetString(result.Buffer);
                
                await ProcessMessageAsync(message, result.RemoteEndPoint);
            }
        }
        catch (OperationCanceledException)
        {
            Log("UDP Server stopping...");
        }
        catch (Exception ex)
        {
            Log($"UDP Server error: {ex.Message}");
        }
    }

    private async Task ProcessMessageAsync(string json, IPEndPoint remoteEndPoint)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var messageType = doc.RootElement.GetProperty("MessageType").GetString();

            switch (messageType)
            {
                case "Subscribe":
                    var subRequest = JsonSerializer.Deserialize<SubscribeRequest>(json);
                    if (subRequest != null)
                    {
                        var key = $"{remoteEndPoint.Address}:{remoteEndPoint.Port}";
                        _subscribers[key] = remoteEndPoint;
                        Log($"UDP subscriber added: {key}");
                        
                        await SendAckAsync(remoteEndPoint, true);
                    }
                    break;

                case "Unsubscribe":
                    var unsubKey = $"{remoteEndPoint.Address}:{remoteEndPoint.Port}";
                    _subscribers.TryRemove(unsubKey, out _);
                    Log($"UDP subscriber removed: {unsubKey}");
                    await SendAckAsync(remoteEndPoint, true);
                    break;

                case "Heartbeat":
                    // Keep connection alive
                    var hbKey = $"{remoteEndPoint.Address}:{remoteEndPoint.Port}";
                    if (_subscribers.ContainsKey(hbKey))
                    {
                        _subscribers[hbKey] = remoteEndPoint;
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Log($"Error processing UDP message: {ex.Message}");
        }
    }

    private async Task SendAckAsync(IPEndPoint endpoint, bool success)
    {
        if (_server == null) return;

        var ack = new AckMessage { Success = success };
        var data = Encoding.UTF8.GetBytes(ack.ToJson());
        await _server.SendAsync(data, data.Length, endpoint);
    }

    /// <summary>
    /// Broadcasts a price update to all subscribers
    /// </summary>
    public async Task BroadcastPriceUpdateAsync(CryptoCurrency crypto)
    {
        if (_server == null || _subscribers.IsEmpty) return;

        var update = new PriceUpdate
        {
            CoinId = crypto.CoinId,
            Symbol = crypto.Symbol,
            Price = crypto.CurrentPrice,
            Change24h = crypto.PriceChangePercentage24h
        };

        var data = Encoding.UTF8.GetBytes(update.ToJson());

        foreach (var subscriber in _subscribers.Values)
        {
            try
            {
                await _server.SendAsync(data, data.Length, subscriber);
            }
            catch (Exception ex)
            {
                Log($"Error broadcasting to {subscriber}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Broadcasts multiple price updates
    /// </summary>
    public async Task BroadcastPriceUpdatesAsync(IEnumerable<CryptoCurrency> cryptos)
    {
        foreach (var crypto in cryptos)
        {
            await BroadcastPriceUpdateAsync(crypto);
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        _server?.Close();
        _subscribers.Clear();
        Log("UDP Server stopped");
    }

    public int SubscriberCount => _subscribers.Count;

    private void Log(string message) => OnLog?.Invoke(this, $"[UDP] {message}");
}
