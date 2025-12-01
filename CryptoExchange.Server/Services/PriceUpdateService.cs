using CryptoTrader.Shared.Models;

namespace CryptoExchange.Server.Services;

/// <summary>
/// Background service that periodically fetches crypto prices and broadcasts updates
/// </summary>
public class PriceUpdateService
{
    private readonly CryptoApiService _cryptoApi;
    private readonly TcpServerService _tcpServer;
    private readonly UdpServerService _udpServer;
    private readonly int _intervalMs;
    private CancellationTokenSource? _cts;
    private List<CryptoCurrency> _lastPrices = new();

    public event EventHandler<string>? OnLog;
    public event EventHandler<List<CryptoCurrency>>? OnPricesUpdated;

    public PriceUpdateService(
        CryptoApiService cryptoApi,
        TcpServerService tcpServer,
        UdpServerService udpServer,
        int intervalMs = 30000)
    {
        _cryptoApi = cryptoApi;
        _tcpServer = tcpServer;
        _udpServer = udpServer;
        _intervalMs = intervalMs;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Log($"Price update service started (interval: {_intervalMs}ms)");

        // Initial fetch
        await UpdatePricesAsync();

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_intervalMs, _cts.Token);
                await UpdatePricesAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log($"Error updating prices: {ex.Message}");
            }
        }
    }

    private async Task UpdatePricesAsync()
    {
        try
        {
            var prices = await _cryptoApi.GetTopCryptosAsync(100);
            
            if (prices.Count > 0)
            {
                _lastPrices = prices;
                
                // Broadcast updates
                var response = new PriceResponse
                {
                    Success = true,
                    Prices = prices
                };
                
                await _tcpServer.BroadcastMessage(response);
                await _udpServer.BroadcastPriceUpdatesAsync(prices);
                
                OnPricesUpdated?.Invoke(this, prices);
                Log($"Updated {prices.Count} crypto prices");
            }
        }
        catch (Exception ex)
        {
            Log($"Error in UpdatePricesAsync: {ex.Message}");
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        Log("Price update service stopped");
    }

    public List<CryptoCurrency> GetLastPrices() => _lastPrices;

    private void Log(string message) => OnLog?.Invoke(this, $"[PriceUpdate] {message}");
}
