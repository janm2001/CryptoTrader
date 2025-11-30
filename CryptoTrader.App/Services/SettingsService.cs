using System;
using System.IO;
using CryptoTrader.Shared.Config;

namespace CryptoTrader.App.Services;

/// <summary>
/// Service for managing application settings using INI file
/// </summary>
public class SettingsService
{
    private static SettingsService? _instance;
    public static SettingsService Instance => _instance ??= new SettingsService();

    private readonly string _settingsPath;
    private readonly IniFile _iniFile;

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, "CryptoTrader");
        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, "settings.ini");
        _iniFile = new IniFile(_settingsPath);
        
        LoadSettings();
    }

    // General Settings
    public string Language { get; set; } = "en";
    public string Theme { get; set; } = "Dark";
    public string DisplayCurrency { get; set; } = "USD";
    
    // Server Settings
    public string ServerAddress { get; set; } = "localhost";
    public int ServerHttpPort { get; set; } = 5002;
    public int ServerTcpPort { get; set; } = 5000;
    public int ServerUdpPort { get; set; } = 5001;
    public bool AutoConnect { get; set; } = true;
    
    // Notification Settings
    public bool PriceAlertsEnabled { get; set; } = true;
    public bool EmailNotificationsEnabled { get; set; } = false;
    public bool SoundEnabled { get; set; } = true;
    
    // Session Settings
    public string? SavedUsername { get; set; }
    public string? SavedToken { get; set; }
    public bool RememberMe { get; set; } = false;
    
    // UI Settings
    public double WindowWidth { get; set; } = 1280;
    public double WindowHeight { get; set; } = 720;
    public bool IsMaximized { get; set; } = false;

    public void LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsPath)) return;

            // General
            Language = _iniFile.GetValue("General", "Language", "en") ?? "en";
            Theme = _iniFile.GetValue("General", "Theme", "Dark") ?? "Dark";
            DisplayCurrency = _iniFile.GetValue("General", "DisplayCurrency", "USD") ?? "USD";

            // Server
            ServerAddress = _iniFile.GetValue("Server", "Address", "localhost") ?? "localhost";
            ServerHttpPort = _iniFile.GetInt("Server", "HttpPort", 5002);
            ServerTcpPort = _iniFile.GetInt("Server", "TcpPort", 5000);
            ServerUdpPort = _iniFile.GetInt("Server", "UdpPort", 5001);
            AutoConnect = _iniFile.GetBool("Server", "AutoConnect", true);

            // Notifications
            PriceAlertsEnabled = _iniFile.GetBool("Notifications", "PriceAlerts", true);
            EmailNotificationsEnabled = _iniFile.GetBool("Notifications", "EmailNotifications", false);
            SoundEnabled = _iniFile.GetBool("Notifications", "Sound", true);

            // Session
            RememberMe = _iniFile.GetBool("Session", "RememberMe", false);
            if (RememberMe)
            {
                SavedUsername = _iniFile.GetValue("Session", "Username", "");
                SavedToken = _iniFile.GetValue("Session", "Token", "");
            }

            // UI
            WindowWidth = double.TryParse(_iniFile.GetValue("Window", "Width", "1280"), out var w) ? w : 1280;
            WindowHeight = double.TryParse(_iniFile.GetValue("Window", "Height", "720"), out var h) ? h : 720;
            IsMaximized = _iniFile.GetBool("Window", "Maximized", false);

            // Apply language
            LanguageService.Instance.CurrentLanguage = Language;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading settings: {ex.Message}");
        }
    }

    public void SaveSettings()
    {
        try
        {
            // General
            _iniFile.SetValue("General", "Language", Language);
            _iniFile.SetValue("General", "Theme", Theme);
            _iniFile.SetValue("General", "DisplayCurrency", DisplayCurrency);

            // Server
            _iniFile.SetValue("Server", "Address", ServerAddress);
            _iniFile.SetInt("Server", "HttpPort", ServerHttpPort);
            _iniFile.SetInt("Server", "TcpPort", ServerTcpPort);
            _iniFile.SetInt("Server", "UdpPort", ServerUdpPort);
            _iniFile.SetBool("Server", "AutoConnect", AutoConnect);

            // Notifications
            _iniFile.SetBool("Notifications", "PriceAlerts", PriceAlertsEnabled);
            _iniFile.SetBool("Notifications", "EmailNotifications", EmailNotificationsEnabled);
            _iniFile.SetBool("Notifications", "Sound", SoundEnabled);

            // Session
            _iniFile.SetBool("Session", "RememberMe", RememberMe);
            if (RememberMe)
            {
                _iniFile.SetValue("Session", "Username", SavedUsername ?? "");
                _iniFile.SetValue("Session", "Token", SavedToken ?? "");
            }
            else
            {
                _iniFile.SetValue("Session", "Username", "");
                _iniFile.SetValue("Session", "Token", "");
            }

            // UI
            _iniFile.SetValue("Window", "Width", WindowWidth.ToString());
            _iniFile.SetValue("Window", "Height", WindowHeight.ToString());
            _iniFile.SetBool("Window", "Maximized", IsMaximized);

            // Save to file
            _iniFile.Save();

            // Update language service
            LanguageService.Instance.CurrentLanguage = Language;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    public void ClearSession()
    {
        SavedUsername = null;
        SavedToken = null;
        RememberMe = false;
        SaveSettings();
    }

    public void SaveSession(string username, string token, bool remember)
    {
        SavedUsername = username;
        SavedToken = token;
        RememberMe = remember;
        SaveSettings();
    }

    public string GetApiBaseUrl() => $"http://{ServerAddress}:{ServerHttpPort}";

    public void ResetToDefaults()
    {
        Language = "en";
        Theme = "Dark";
        DisplayCurrency = "USD";
        ServerAddress = "localhost";
        ServerHttpPort = 5002;
        ServerTcpPort = 5000;
        ServerUdpPort = 5001;
        AutoConnect = true;
        PriceAlertsEnabled = true;
        EmailNotificationsEnabled = false;
        SoundEnabled = true;
        WindowWidth = 1280;
        WindowHeight = 720;
        IsMaximized = false;
        
        SaveSettings();
    }
}
