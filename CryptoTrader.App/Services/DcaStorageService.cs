using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using CryptoTrader.App.Models;

namespace CryptoTrader.App.Services;

/// <summary>
/// Service for managing DCA plans with XML and JSON storage
/// Supports CRUD operations: Create, Read, Update, Delete
/// </summary>
public class DcaStorageService
{
    private static DcaStorageService? _instance;
    public static DcaStorageService Instance => _instance ??= new DcaStorageService();

    private readonly string _dataFolder;
    private readonly string _xmlFilePath;
    private readonly string _jsonFilePath;
    private DcaPlanCollection _plans;

    public event EventHandler<DcaPlan>? OnPlanAdded;
    public event EventHandler<DcaPlan>? OnPlanUpdated;
    public event EventHandler<string>? OnPlanDeleted;
    public event EventHandler? OnPlansChanged;

    private DcaStorageService()
    {
        _dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CryptoTrader", "DCA");
        Directory.CreateDirectory(_dataFolder);

        _xmlFilePath = Path.Combine(_dataFolder, "dca_plans.xml");
        _jsonFilePath = Path.Combine(_dataFolder, "dca_plans.json");

        _plans = new DcaPlanCollection();
    }

    #region Load/Save Operations

    /// <summary>
    /// Load plans from both XML and JSON (prefers JSON if both exist)
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            // Try JSON first (primary format)
            if (File.Exists(_jsonFilePath))
            {
                await LoadFromJsonAsync();
                return;
            }

            // Fallback to XML
            if (File.Exists(_xmlFilePath))
            {
                await LoadFromXmlAsync();
                // Also save to JSON for next time
                await SaveToJsonAsync();
                return;
            }

            // No existing data, start fresh
            _plans = new DcaPlanCollection();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading DCA plans: {ex.Message}");
            _plans = new DcaPlanCollection();
        }
    }

    /// <summary>
    /// Load plans from JSON file
    /// </summary>
    public async Task LoadFromJsonAsync()
    {
        var json = await File.ReadAllTextAsync(_jsonFilePath);
        _plans = JsonSerializer.Deserialize<DcaPlanCollection>(json) ?? new DcaPlanCollection();
        OnPlansChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Load plans from XML file
    /// </summary>
    public async Task LoadFromXmlAsync()
    {
        await Task.Run(() =>
        {
            using var stream = File.OpenRead(_xmlFilePath);
            var serializer = new XmlSerializer(typeof(DcaPlanCollection));
            _plans = (DcaPlanCollection?)serializer.Deserialize(stream) ?? new DcaPlanCollection();
        });
        OnPlansChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Save plans to both XML and JSON formats
    /// </summary>
    public async Task SaveAsync()
    {
        _plans.LastUpdated = DateTime.UtcNow;
        await Task.WhenAll(SaveToJsonAsync(), SaveToXmlAsync());
    }

    /// <summary>
    /// Save plans to JSON file
    /// </summary>
    public async Task SaveToJsonAsync()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(_plans, options);
        await File.WriteAllTextAsync(_jsonFilePath, json);
    }

    /// <summary>
    /// Save plans to XML file
    /// </summary>
    public async Task SaveToXmlAsync()
    {
        await Task.Run(() =>
        {
            using var stream = File.Create(_xmlFilePath);
            var serializer = new XmlSerializer(typeof(DcaPlanCollection));
            serializer.Serialize(stream, _plans);
        });
    }

    #endregion

    #region CRUD Operations

    /// <summary>
    /// Get all DCA plans
    /// </summary>
    public IReadOnlyList<DcaPlan> GetAllPlans() => _plans.Plans.AsReadOnly();

    /// <summary>
    /// Get active DCA plans
    /// </summary>
    public IReadOnlyList<DcaPlan> GetActivePlans() => _plans.Plans.Where(p => p.IsActive).ToList().AsReadOnly();

    /// <summary>
    /// Get a specific plan by ID
    /// </summary>
    public DcaPlan? GetPlanById(string id) => _plans.Plans.FirstOrDefault(p => p.Id == id);

    /// <summary>
    /// Get plans for a specific coin
    /// </summary>
    public IReadOnlyList<DcaPlan> GetPlansByCoin(string coinId) => 
        _plans.Plans.Where(p => p.CoinId == coinId).ToList().AsReadOnly();

    /// <summary>
    /// Create a new DCA plan
    /// </summary>
    public async Task<DcaPlan> CreatePlanAsync(DcaPlan plan)
    {
        plan.Id = Guid.NewGuid().ToString();
        plan.CreatedAt = DateTime.UtcNow;
        plan.CalculateNextExecution();

        _plans.Plans.Add(plan);
        await SaveAsync();

        OnPlanAdded?.Invoke(this, plan);
        OnPlansChanged?.Invoke(this, EventArgs.Empty);

        return plan;
    }

    /// <summary>
    /// Update an existing DCA plan
    /// </summary>
    public async Task<bool> UpdatePlanAsync(DcaPlan updatedPlan)
    {
        var existingIndex = _plans.Plans.FindIndex(p => p.Id == updatedPlan.Id);
        if (existingIndex < 0)
            return false;

        updatedPlan.CalculateNextExecution();
        _plans.Plans[existingIndex] = updatedPlan;
        await SaveAsync();

        OnPlanUpdated?.Invoke(this, updatedPlan);
        OnPlansChanged?.Invoke(this, EventArgs.Empty);

        return true;
    }

    /// <summary>
    /// Delete a DCA plan
    /// </summary>
    public async Task<bool> DeletePlanAsync(string planId)
    {
        var plan = _plans.Plans.FirstOrDefault(p => p.Id == planId);
        if (plan == null)
            return false;

        _plans.Plans.Remove(plan);
        await SaveAsync();

        OnPlanDeleted?.Invoke(this, planId);
        OnPlansChanged?.Invoke(this, EventArgs.Empty);

        return true;
    }

    /// <summary>
    /// Toggle plan active status
    /// </summary>
    public async Task<bool> TogglePlanActiveAsync(string planId)
    {
        var plan = _plans.Plans.FirstOrDefault(p => p.Id == planId);
        if (plan == null)
            return false;

        plan.IsActive = !plan.IsActive;
        await SaveAsync();

        OnPlanUpdated?.Invoke(this, plan);
        OnPlansChanged?.Invoke(this, EventArgs.Empty);

        return true;
    }

    #endregion

    #region Execution Operations

    /// <summary>
    /// Get plans that are due for execution
    /// </summary>
    public IReadOnlyList<DcaPlan> GetDuePlans()
    {
        var now = DateTime.UtcNow;
        return _plans.Plans
            .Where(p => p.IsActive && p.NextExecution <= now)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Record a plan execution
    /// </summary>
    public async Task RecordExecutionAsync(string planId, DcaExecution execution)
    {
        var plan = _plans.Plans.FirstOrDefault(p => p.Id == planId);
        if (plan == null)
            return;

        plan.ExecutionHistory.Add(execution);
        plan.ExecutionCount++;
        plan.LastExecuted = execution.ExecutedAt;

        if (execution.Status == DcaExecutionStatus.Completed)
        {
            plan.TotalInvested += execution.Amount;
            plan.TotalCoinsBought += execution.CoinsBought;
        }

        plan.CalculateNextExecution();
        await SaveAsync();

        OnPlanUpdated?.Invoke(this, plan);
        OnPlansChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Export/Import

    /// <summary>
    /// Export plans to a specific JSON file
    /// </summary>
    public async Task ExportToJsonAsync(string filePath)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(_plans, options);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Export plans to a specific XML file
    /// </summary>
    public async Task ExportToXmlAsync(string filePath)
    {
        await Task.Run(() =>
        {
            using var stream = File.Create(filePath);
            var serializer = new XmlSerializer(typeof(DcaPlanCollection));
            serializer.Serialize(stream, _plans);
        });
    }

    /// <summary>
    /// Import plans from a JSON file (merges with existing)
    /// </summary>
    public async Task ImportFromJsonAsync(string filePath, bool replaceExisting = false)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var imported = JsonSerializer.Deserialize<DcaPlanCollection>(json);

        if (imported != null)
        {
            await MergePlansAsync(imported.Plans, replaceExisting);
        }
    }

    /// <summary>
    /// Import plans from an XML file (merges with existing)
    /// </summary>
    public async Task ImportFromXmlAsync(string filePath, bool replaceExisting = false)
    {
        DcaPlanCollection? imported = null;
        await Task.Run(() =>
        {
            using var stream = File.OpenRead(filePath);
            var serializer = new XmlSerializer(typeof(DcaPlanCollection));
            imported = (DcaPlanCollection?)serializer.Deserialize(stream);
        });

        if (imported != null)
        {
            await MergePlansAsync(imported.Plans, replaceExisting);
        }
    }

    private async Task MergePlansAsync(List<DcaPlan> newPlans, bool replaceExisting)
    {
        if (replaceExisting)
        {
            _plans.Plans.Clear();
            _plans.Plans.AddRange(newPlans);
        }
        else
        {
            foreach (var plan in newPlans)
            {
                var existing = _plans.Plans.FirstOrDefault(p => p.Id == plan.Id);
                if (existing != null)
                {
                    _plans.Plans.Remove(existing);
                }
                _plans.Plans.Add(plan);
            }
        }

        await SaveAsync();
        OnPlansChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Get summary statistics for all plans
    /// </summary>
    public DcaSummary GetSummary()
    {
        return new DcaSummary
        {
            TotalPlans = _plans.Plans.Count,
            ActivePlans = _plans.Plans.Count(p => p.IsActive),
            TotalInvested = _plans.Plans.Sum(p => p.TotalInvested),
            TotalExecutions = _plans.Plans.Sum(p => p.ExecutionCount),
            UniqueCoinCount = _plans.Plans.Select(p => p.CoinId).Distinct().Count()
        };
    }

    #endregion

    /// <summary>
    /// Get the data folder path
    /// </summary>
    public string DataFolder => _dataFolder;
}

/// <summary>
/// Summary statistics for DCA plans
/// </summary>
public class DcaSummary
{
    public int TotalPlans { get; set; }
    public int ActivePlans { get; set; }
    public decimal TotalInvested { get; set; }
    public int TotalExecutions { get; set; }
    public int UniqueCoinCount { get; set; }
}
