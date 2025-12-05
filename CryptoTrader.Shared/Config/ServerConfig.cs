using System;
using System.IO;

namespace CryptoTrader.Shared.Config;

/// <summary>
/// Server configuration - loads from server.ini or environment variables
/// </summary>
public class ServerConfig
{
    public int TcpPort { get; set; } = 5000;
    public int UdpPort { get; set; } = 5001;
    public int HttpPort { get; set; } = 5002;
    public string DatabasePath { get; set; } = "cryptotrader.db";
    public string CryptoApiBaseUrl { get; set; } = "https://api.coingecko.com/api/v3";
    
    /// <summary>
    /// CoinGecko Demo API Key - Get free at https://www.coingecko.com/en/api/pricing
    /// Required since late 2024 for API access
    /// Can be set via:
    /// 1. server.ini file: CoinGeckoApiKey=YOUR_KEY
    /// 2. Environment variable: COINGECKO_API_KEY
    /// </summary>
    public string? CoinGeckoApiKey { get; set; } = null;
    
    public int PriceUpdateIntervalMs { get; set; } = 60000; // 60 seconds (Demo API has lower limits)
    public int MaxConnections { get; set; } = 100;
    public int TokenExpirationHours { get; set; } = 24;
    public int SessionTimeoutMinutes { get; set; } = 60;

    public ServerConfig()
    {
        LoadFromFile();
        LoadFromEnvironment();
    }

    private void LoadFromFile()
    {
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server.ini");
        if (!File.Exists(configPath))
        {
            // Also check current directory
            configPath = "server.ini";
        }
        
        if (!File.Exists(configPath))
            return;

        var ini = new IniFile(configPath);
        
        TcpPort = ini.GetInt("Server", "TcpPort", TcpPort);
        UdpPort = ini.GetInt("Server", "UdpPort", UdpPort);
        HttpPort = ini.GetInt("Server", "HttpPort", HttpPort);
        DatabasePath = ini.GetValue("Server", "DatabasePath", DatabasePath) ?? DatabasePath;
        CryptoApiBaseUrl = ini.GetValue("API", "CryptoApiBaseUrl", CryptoApiBaseUrl) ?? CryptoApiBaseUrl;
        CoinGeckoApiKey = ini.GetValue("API", "CoinGeckoApiKey", null);
        PriceUpdateIntervalMs = ini.GetInt("Server", "PriceUpdateIntervalMs", PriceUpdateIntervalMs);
        MaxConnections = ini.GetInt("Server", "MaxConnections", MaxConnections);
        TokenExpirationHours = ini.GetInt("Auth", "TokenExpirationHours", TokenExpirationHours);
        SessionTimeoutMinutes = ini.GetInt("Auth", "SessionTimeoutMinutes", SessionTimeoutMinutes);
    }

    private void LoadFromEnvironment()
    {
        // Environment variables override config file
        var envApiKey = Environment.GetEnvironmentVariable("COINGECKO_API_KEY");
        if (!string.IsNullOrEmpty(envApiKey))
        {
            CoinGeckoApiKey = envApiKey;
        }

        var envHttpPort = Environment.GetEnvironmentVariable("HTTP_PORT");
        if (int.TryParse(envHttpPort, out var httpPort))
        {
            HttpPort = httpPort;
        }
    }
}
