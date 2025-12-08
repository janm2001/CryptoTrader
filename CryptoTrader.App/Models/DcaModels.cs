using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Text.Json.Serialization;

namespace CryptoTrader.App.Models;

/// <summary>
/// Represents a Dollar Cost Averaging (DCA) investment plan
/// </summary>
[XmlRoot("DcaPlan")]
public class DcaPlan
{
    [XmlAttribute("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [XmlElement("UserId")]
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = "";

    [XmlElement("Name")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [XmlElement("CoinId")]
    [JsonPropertyName("coinId")]
    public string CoinId { get; set; } = "";

    [XmlElement("Symbol")]
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = "";

    [XmlElement("Amount")]
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [XmlElement("Frequency")]
    [JsonPropertyName("frequency")]
    public DcaFrequency Frequency { get; set; } = DcaFrequency.Weekly;

    [XmlElement("DayOfWeek")]
    [JsonPropertyName("dayOfWeek")]
    public DayOfWeek DayOfWeek { get; set; } = DayOfWeek.Monday;

    [XmlElement("DayOfMonth")]
    [JsonPropertyName("dayOfMonth")]
    public int DayOfMonth { get; set; } = 1;

    [XmlElement("IsActive")]
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [XmlElement("CreatedAt")]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [XmlElement("LastExecuted")]
    [JsonPropertyName("lastExecuted")]
    public DateTime? LastExecuted { get; set; }

    [XmlElement("NextExecution")]
    [JsonPropertyName("nextExecution")]
    public DateTime NextExecution { get; set; }

    [XmlElement("TotalInvested")]
    [JsonPropertyName("totalInvested")]
    public decimal TotalInvested { get; set; }

    [XmlElement("TotalCoinsBought")]
    [JsonPropertyName("totalCoinsBought")]
    public decimal TotalCoinsBought { get; set; }

    [XmlElement("ExecutionCount")]
    [JsonPropertyName("executionCount")]
    public int ExecutionCount { get; set; }

    [XmlArray("ExecutionHistory")]
    [XmlArrayItem("Execution")]
    [JsonPropertyName("executionHistory")]
    public List<DcaExecution> ExecutionHistory { get; set; } = new();

    /// <summary>
    /// Calculate the next execution date based on frequency
    /// </summary>
    public void CalculateNextExecution()
    {
        var baseDate = LastExecuted ?? DateTime.UtcNow;

        NextExecution = Frequency switch
        {
            DcaFrequency.Daily => baseDate.AddDays(1),
            DcaFrequency.Weekly => GetNextWeekday(baseDate, DayOfWeek),
            DcaFrequency.BiWeekly => GetNextWeekday(baseDate.AddDays(14), DayOfWeek),
            DcaFrequency.Monthly => GetNextMonthDay(baseDate, DayOfMonth),
            _ => baseDate.AddDays(7)
        };
    }

    private static DateTime GetNextWeekday(DateTime start, DayOfWeek day)
    {
        var daysToAdd = ((int)day - (int)start.DayOfWeek + 7) % 7;
        if (daysToAdd == 0) daysToAdd = 7;
        return start.AddDays(daysToAdd).Date;
    }

    private static DateTime GetNextMonthDay(DateTime start, int dayOfMonth)
    {
        var next = new DateTime(start.Year, start.Month, Math.Min(dayOfMonth, DateTime.DaysInMonth(start.Year, start.Month)));
        if (next <= start)
        {
            next = next.AddMonths(1);
            next = new DateTime(next.Year, next.Month, Math.Min(dayOfMonth, DateTime.DaysInMonth(next.Year, next.Month)));
        }
        return next;
    }

    /// <summary>
    /// Calculate average purchase price
    /// </summary>
    public decimal AveragePurchasePrice => TotalCoinsBought > 0 ? TotalInvested / TotalCoinsBought : 0;
}

/// <summary>
/// Represents a single DCA execution/purchase
/// </summary>
public class DcaExecution
{
    [XmlAttribute("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [XmlElement("ExecutedAt")]
    [JsonPropertyName("executedAt")]
    public DateTime ExecutedAt { get; set; }

    [XmlElement("Amount")]
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [XmlElement("PricePerCoin")]
    [JsonPropertyName("pricePerCoin")]
    public decimal PricePerCoin { get; set; }

    [XmlElement("CoinsBought")]
    [JsonPropertyName("coinsBought")]
    public decimal CoinsBought { get; set; }

    [XmlElement("Status")]
    [JsonPropertyName("status")]
    public DcaExecutionStatus Status { get; set; }

    [XmlElement("Notes")]
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

/// <summary>
/// Frequency of DCA investments
/// </summary>
public enum DcaFrequency
{
    Daily,
    Weekly,
    BiWeekly,
    Monthly
}

/// <summary>
/// Status of a DCA execution
/// </summary>
public enum DcaExecutionStatus
{
    Pending,
    Completed,
    Failed,
    Skipped,
    InsufficientFunds
}

/// <summary>
/// Container for multiple DCA plans (for XML serialization)
/// </summary>
[XmlRoot("DcaPlans")]
public class DcaPlanCollection
{
    [XmlAttribute("version")]
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [XmlAttribute("lastUpdated")]
    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    [XmlArray("Plans")]
    [XmlArrayItem("Plan")]
    [JsonPropertyName("plans")]
    public List<DcaPlan> Plans { get; set; } = new();
}
