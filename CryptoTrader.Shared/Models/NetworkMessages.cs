using System.Text.Json;

namespace CryptoTrader.Shared.Models;

/// <summary>
/// Base class for all network messages
/// </summary>
public abstract class NetworkMessage
{
    public string MessageType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }
    
    public string ToJson() => JsonSerializer.Serialize(this, GetType());
    
    public static T? FromJson<T>(string json) where T : NetworkMessage
        => JsonSerializer.Deserialize<T>(json);
}

/// <summary>
/// Request for crypto price data
/// </summary>
public class PriceRequest : NetworkMessage
{
    public PriceRequest() => MessageType = "PriceRequest";
    
    public List<string> CoinIds { get; set; } = new();
    public string Currency { get; set; } = "usd";
}

/// <summary>
/// Response with crypto price data
/// </summary>
public class PriceResponse : NetworkMessage
{
    public PriceResponse() => MessageType = "PriceResponse";
    
    public List<CryptoCurrency> Prices { get; set; } = new();
    public bool Success { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Real-time price update (sent via UDP for low latency)
/// </summary>
public class PriceUpdate : NetworkMessage
{
    public PriceUpdate() => MessageType = "PriceUpdate";
    
    public string CoinId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Change24h { get; set; }
}

/// <summary>
/// Subscribe to price updates
/// </summary>
public class SubscribeRequest : NetworkMessage
{
    public SubscribeRequest() => MessageType = "Subscribe";
    
    public List<string> CoinIds { get; set; } = new();
}

/// <summary>
/// Unsubscribe from price updates
/// </summary>
public class UnsubscribeRequest : NetworkMessage
{
    public UnsubscribeRequest() => MessageType = "Unsubscribe";
    
    public List<string> CoinIds { get; set; } = new();
}

/// <summary>
/// Authentication request for TCP connection
/// </summary>
public class AuthRequest : NetworkMessage
{
    public AuthRequest() => MessageType = "Auth";
    
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// Server acknowledgment
/// </summary>
public class AckMessage : NetworkMessage
{
    public AckMessage() => MessageType = "Ack";
    
    public bool Success { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Heartbeat/ping message
/// </summary>
public class HeartbeatMessage : NetworkMessage
{
    public HeartbeatMessage() => MessageType = "Heartbeat";
}
