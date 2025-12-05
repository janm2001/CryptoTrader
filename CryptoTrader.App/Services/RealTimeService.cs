using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using CryptoTrader.Shared.Models;

namespace CryptoTrader.App.Services;

/// <summary>
/// Service for real-time price updates via TCP and UDP connections
/// </summary>
public class RealTimeService : IDisposable
{
    private static RealTimeService? _instance;
    public static RealTimeService Instance => _instance ??= new RealTimeService();

    private TcpClient? _tcpClient;
    private UdpClient? _udpClient;
    private NetworkStream? _tcpStream;
    private CancellationTokenSource? _cts;
    private bool _isConnected;
    private string? _authToken;

    public event EventHandler<List<CryptoCurrency>>? OnPricesUpdated;
    public event EventHandler<string>? OnConnectionStatusChanged;
    public event EventHandler<string>? OnError;

    public bool IsConnected => _isConnected;

    private RealTimeService() { }

    /// <summary>
    /// Connect to the TCP server for real-time updates
    /// </summary>
    public async Task ConnectTcpAsync(string host, int port, string? authToken = null)
    {
        _authToken = authToken;
        _cts = new CancellationTokenSource();

        try
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(host, port);
            _tcpStream = _tcpClient.GetStream();
            _isConnected = true;

            OnConnectionStatusChanged?.Invoke(this, "Connected to TCP server");

            // Authenticate if we have a token
            if (!string.IsNullOrEmpty(_authToken))
            {
                var authRequest = new TcpAuthRequest
                {
                    MessageType = "Auth",
                    Token = _authToken
                };
                await SendTcpMessageAsync(authRequest);
            }

            // Subscribe to price updates
            var subscribeRequest = new TcpSubscribeRequest
            {
                MessageType = "Subscribe",
                Channels = new List<string> { "prices" }
            };
            await SendTcpMessageAsync(subscribeRequest);

            // Start listening for messages
            _ = ListenTcpAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            _isConnected = false;
            OnError?.Invoke(this, $"TCP connection failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Connect to the UDP server for broadcast price updates
    /// </summary>
    public async Task ConnectUdpAsync(string host, int port)
    {
        _cts ??= new CancellationTokenSource();

        try
        {
            _udpClient = new UdpClient();
            _udpClient.Connect(host, port);

            // Send subscribe message
            var subscribeRequest = new UdpSubscribeRequest
            {
                MessageType = "Subscribe"
            };
            var json = JsonSerializer.Serialize(subscribeRequest);
            var data = Encoding.UTF8.GetBytes(json);
            await _udpClient.SendAsync(data, data.Length);

            OnConnectionStatusChanged?.Invoke(this, "Subscribed to UDP broadcasts");

            // Start listening for UDP broadcasts
            _ = ListenUdpAsync(_cts.Token);

            // Start heartbeat
            _ = SendUdpHeartbeatAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"UDP connection failed: {ex.Message}");
        }
    }

    private async Task ListenTcpAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        var messageBuilder = new StringBuilder();

        try
        {
            while (!cancellationToken.IsCancellationRequested && _tcpClient?.Connected == true)
            {
                var bytesRead = await _tcpStream!.ReadAsync(buffer, cancellationToken);
                if (bytesRead == 0) break;

                var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                ProcessTcpMessage(message);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"TCP receive error: {ex.Message}");
        }
        finally
        {
            _isConnected = false;
            OnConnectionStatusChanged?.Invoke(this, "Disconnected from TCP server");
        }
    }

    private async Task ListenUdpAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _udpClient != null)
            {
                var result = await _udpClient.ReceiveAsync(cancellationToken);
                var message = Encoding.UTF8.GetString(result.Buffer);
                ProcessUdpMessage(message);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"UDP receive error: {ex.Message}");
        }
    }

    private async Task SendUdpHeartbeatAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _udpClient != null)
            {
                await Task.Delay(30000, cancellationToken); // Every 30 seconds

                var heartbeat = new { MessageType = "Heartbeat" };
                var json = JsonSerializer.Serialize(heartbeat);
                var data = Encoding.UTF8.GetBytes(json);
                await _udpClient.SendAsync(data, data.Length);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
    }

    private void ProcessTcpMessage(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("MessageType", out var messageType))
                return;

            switch (messageType.GetString())
            {
                case "PriceUpdate":
                    var priceResponse = JsonSerializer.Deserialize<PriceResponse>(json);
                    if (priceResponse?.Prices != null)
                    {
                        OnPricesUpdated?.Invoke(this, priceResponse.Prices);
                    }
                    break;

                case "AuthResponse":
                    // Handle auth response if needed
                    break;
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"Error processing TCP message: {ex.Message}");
        }
    }

    private void ProcessUdpMessage(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("MessageType", out var messageType))
                return;

            switch (messageType.GetString())
            {
                case "PriceBroadcast":
                    var priceResponse = JsonSerializer.Deserialize<PriceResponse>(json);
                    if (priceResponse?.Prices != null)
                    {
                        OnPricesUpdated?.Invoke(this, priceResponse.Prices);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"Error processing UDP message: {ex.Message}");
        }
    }

    private async Task SendTcpMessageAsync(object message)
    {
        if (_tcpStream == null || !_tcpClient?.Connected == true)
            return;

        var json = JsonSerializer.Serialize(message, message.GetType());
        var data = Encoding.UTF8.GetBytes(json);
        await _tcpStream.WriteAsync(data);
    }

    public void Disconnect()
    {
        _cts?.Cancel();
        _tcpStream?.Close();
        _tcpClient?.Close();
        _udpClient?.Close();
        _isConnected = false;
        OnConnectionStatusChanged?.Invoke(this, "Disconnected");
    }

    public void Dispose()
    {
        Disconnect();
        _cts?.Dispose();
        _tcpStream?.Dispose();
        _tcpClient?.Dispose();
        _udpClient?.Dispose();
    }
}

// TCP/UDP message models
public class TcpAuthRequest
{
    public string MessageType { get; set; } = "Auth";
    public string Token { get; set; } = "";
}

public class TcpSubscribeRequest
{
    public string MessageType { get; set; } = "Subscribe";
    public List<string> Channels { get; set; } = new();
}

public class UdpSubscribeRequest
{
    public string MessageType { get; set; } = "Subscribe";
}
