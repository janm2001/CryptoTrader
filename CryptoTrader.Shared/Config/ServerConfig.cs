namespace CryptoTrader.Shared.Config;

/// <summary>
/// Server configuration
/// </summary>
public class ServerConfig
{
    public int TcpPort { get; set; } = 5000;
    public int UdpPort { get; set; } = 5001;
    public int HttpPort { get; set; } = 5002;
    public string DatabasePath { get; set; } = "cryptotrader.db";
    public string CryptoApiBaseUrl { get; set; } = "https://api.coingecko.com/api/v3";
    public int PriceUpdateIntervalMs { get; set; } = 30000; // 30 seconds
    public int MaxConnections { get; set; } = 100;
    public int TokenExpirationHours { get; set; } = 24;
    public int SessionTimeoutMinutes { get; set; } = 60;
}
