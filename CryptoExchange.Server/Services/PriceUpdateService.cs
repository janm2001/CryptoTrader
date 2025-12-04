using CryptoExchange.Server.Data;
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
    private readonly DatabaseContext _db;
    private readonly int _intervalMs;
    private readonly int _historyIntervalMs;
    private CancellationTokenSource? _cts;
    private List<CryptoCurrency> _lastPrices = new();
    private DateTime _lastHistorySave = DateTime.MinValue;

    public event EventHandler<string>? OnLog;
    public event EventHandler<List<CryptoCurrency>>? OnPricesUpdated;

    public PriceUpdateService(
        CryptoApiService cryptoApi,
        TcpServerService tcpServer,
        UdpServerService udpServer,
        DatabaseContext db,
        int intervalMs = 30000,
        int historyIntervalHours = 1)
    {
        _cryptoApi = cryptoApi;
        _tcpServer = tcpServer;
        _udpServer = udpServer;
        _db = db;
        _intervalMs = intervalMs;
        _historyIntervalMs = historyIntervalHours * 60 * 60 * 1000; // Convert hours to milliseconds
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
                
                // Save to price history if enough time has passed (default: every hour)
                if ((DateTime.UtcNow - _lastHistorySave).TotalMilliseconds >= _historyIntervalMs)
                {
                    await SavePriceHistoryAsync(prices);
                    _lastHistorySave = DateTime.UtcNow;
                }
                
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

    private async Task SavePriceHistoryAsync(List<CryptoCurrency> prices)
    {
        try
        {
            int savedCount = 0;
            foreach (var crypto in prices.Take(20)) // Save top 20 coins to history
            {
                await _db.AddPriceHistoryAsync(
                    crypto.CoinId,
                    crypto.CurrentPrice,
                    crypto.MarketCap,
                    crypto.TotalVolume);
                savedCount++;
            }
            Log($"Saved price history for {savedCount} cryptocurrencies");
        }
        catch (Exception ex)
        {
            Log($"Error saving price history: {ex.Message}");
        }
    }

    /// <summary>
    /// Forces an immediate save of price history (useful on app startup)
    /// </summary>
    public async Task ForceSavePriceHistoryAsync()
    {
        if (_lastPrices.Count > 0)
        {
            await SavePriceHistoryAsync(_lastPrices);
            _lastHistorySave = DateTime.UtcNow;
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
