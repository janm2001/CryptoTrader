using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using CryptoTrader.Shared.Models;
using CryptoExchange.Server.Data;

namespace CryptoExchange.Server.Services;

/// <summary>
/// TCP Server for handling client connections and requests
/// </summary>
public class TcpServerService
{
    private readonly int _port;
    private readonly DatabaseContext _db;
    private readonly AuthService _auth;
    private readonly CryptoApiService _cryptoApi;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly ConcurrentDictionary<string, TcpClientHandler> _clients = new();

    public event EventHandler<string>? OnLog;

    public TcpServerService(int port, DatabaseContext db, AuthService auth, CryptoApiService cryptoApi)
    {
        _port = port;
        _db = db;
        _auth = auth;
        _cryptoApi = cryptoApi;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();

        Log($"TCP Server started on port {_port}");

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                var clientId = Guid.NewGuid().ToString();
                var handler = new TcpClientHandler(clientId, client, _db, _auth, _cryptoApi);
                handler.OnLog += (s, msg) => Log(msg);
                handler.OnDisconnected += (s, id) => _clients.TryRemove(id, out _);
                
                _clients[clientId] = handler;
                _ = handler.HandleAsync(_cts.Token);
                
                Log($"Client connected: {clientId}");
            }
        }
        catch (OperationCanceledException)
        {
            Log("TCP Server stopping...");
        }
        catch (Exception ex)
        {
            Log($"TCP Server error: {ex.Message}");
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        
        foreach (var client in _clients.Values)
        {
            client.Disconnect();
        }
        _clients.Clear();
        
        _listener?.Stop();
        Log("TCP Server stopped");
    }

    public async Task BroadcastMessage(NetworkMessage message)
    {
        var json = JsonSerializer.Serialize(message, message.GetType());
        foreach (var client in _clients.Values)
        {
            await client.SendMessageAsync(json);
        }
    }

    public int ConnectedClients => _clients.Count;

    private void Log(string message) => OnLog?.Invoke(this, $"[TCP] {message}");
}

/// <summary>
/// Handles individual TCP client connections
/// </summary>
public class TcpClientHandler
{
    private readonly string _clientId;
    private readonly TcpClient _client;
    private readonly DatabaseContext _db;
    private readonly AuthService _auth;
    private readonly CryptoApiService _cryptoApi;
    private NetworkStream? _stream;
    private UserSession? _session;
    private readonly List<string> _subscriptions = new();

    public event EventHandler<string>? OnLog;
    public event EventHandler<string>? OnDisconnected;

    public TcpClientHandler(string clientId, TcpClient client, DatabaseContext db, AuthService auth, CryptoApiService cryptoApi)
    {
        _clientId = clientId;
        _client = client;
        _db = db;
        _auth = auth;
        _cryptoApi = cryptoApi;
    }

    public async Task HandleAsync(CancellationToken cancellationToken)
    {
        try
        {
            _stream = _client.GetStream();
            var buffer = new byte[8192];

            while (!cancellationToken.IsCancellationRequested && _client.Connected)
            {
                var bytesRead = await _stream.ReadAsync(buffer, cancellationToken);
                if (bytesRead == 0) break;

                var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                await ProcessMessageAsync(message);
            }
        }
        catch (Exception ex)
        {
            Log($"Client {_clientId} error: {ex.Message}");
        }
        finally
        {
            Disconnect();
        }
    }

    private async Task ProcessMessageAsync(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var messageType = doc.RootElement.GetProperty("MessageType").GetString();

            switch (messageType)
            {
                case "Auth":
                    await HandleAuthAsync(json);
                    break;
                case "PriceRequest":
                    await HandlePriceRequestAsync(json);
                    break;
                case "Subscribe":
                    await HandleSubscribeAsync(json);
                    break;
                case "Unsubscribe":
                    await HandleUnsubscribeAsync(json);
                    break;
                case "Heartbeat":
                    await SendMessageAsync(new HeartbeatMessage().ToJson());
                    break;
                default:
                    await SendMessageAsync(new AckMessage { Success = false, Error = "Unknown message type" }.ToJson());
                    break;
            }
        }
        catch (Exception ex)
        {
            Log($"Error processing message: {ex.Message}");
            await SendMessageAsync(new AckMessage { Success = false, Error = ex.Message }.ToJson());
        }
    }

    private async Task HandleAuthAsync(string json)
    {
        var request = JsonSerializer.Deserialize<AuthRequest>(json);
        if (request == null)
        {
            await SendMessageAsync(new AckMessage { Success = false, Error = "Invalid auth request" }.ToJson());
            return;
        }

        var result = await _auth.ValidateTokenAsync(request.Token);
        if (result.Success)
        {
            _session = result.Session;
            await SendMessageAsync(new AckMessage { Success = true }.ToJson());
            Log($"Client {_clientId} authenticated as {_session?.Username}");
        }
        else
        {
            await SendMessageAsync(new AckMessage { Success = false, Error = result.Message }.ToJson());
        }
    }

    private async Task HandlePriceRequestAsync(string json)
    {
        var request = JsonSerializer.Deserialize<PriceRequest>(json);
        if (request == null)
        {
            await SendMessageAsync(new PriceResponse { Success = false, Error = "Invalid request" }.ToJson());
            return;
        }

        var prices = request.CoinIds.Count > 0
            ? await _cryptoApi.GetCryptosByIdsAsync(request.CoinIds, request.Currency)
            : await _cryptoApi.GetTopCryptosAsync(50, request.Currency);

        var response = new PriceResponse
        {
            Success = true,
            Prices = prices,
            CorrelationId = request.CorrelationId
        };

        await SendMessageAsync(response.ToJson());
    }

    private async Task HandleSubscribeAsync(string json)
    {
        var request = JsonSerializer.Deserialize<SubscribeRequest>(json);
        if (request == null) return;

        _subscriptions.AddRange(request.CoinIds.Where(id => !_subscriptions.Contains(id)));
        await SendMessageAsync(new AckMessage { Success = true, CorrelationId = request.CorrelationId }.ToJson());
        Log($"Client {_clientId} subscribed to: {string.Join(", ", request.CoinIds)}");
    }

    private async Task HandleUnsubscribeAsync(string json)
    {
        var request = JsonSerializer.Deserialize<UnsubscribeRequest>(json);
        if (request == null) return;

        foreach (var coinId in request.CoinIds)
        {
            _subscriptions.Remove(coinId);
        }
        await SendMessageAsync(new AckMessage { Success = true, CorrelationId = request.CorrelationId }.ToJson());
    }

    public async Task SendMessageAsync(string json)
    {
        if (_stream == null || !_client.Connected) return;

        try
        {
            var data = Encoding.UTF8.GetBytes(json + "\n");
            await _stream.WriteAsync(data);
        }
        catch (Exception ex)
        {
            Log($"Error sending message: {ex.Message}");
        }
    }

    public void Disconnect()
    {
        try
        {
            _stream?.Close();
            _client.Close();
        }
        catch { }
        
        OnDisconnected?.Invoke(this, _clientId);
        Log($"Client {_clientId} disconnected");
    }

    public IReadOnlyList<string> Subscriptions => _subscriptions.AsReadOnly();

    private void Log(string message) => OnLog?.Invoke(this, message);
}
