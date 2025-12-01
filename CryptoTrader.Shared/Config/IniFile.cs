using System.IO;

namespace CryptoTrader.Shared.Config;

/// <summary>
/// INI file parser and writer for storing user settings
/// </summary>
public class IniFile
{
    private readonly string _filePath;
    private readonly Dictionary<string, Dictionary<string, string>> _sections;

    public IniFile(string filePath)
    {
        _filePath = filePath;
        _sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        
        if (File.Exists(filePath))
        {
            Load();
        }
    }

    public void Load()
    {
        _sections.Clear();
        
        if (!File.Exists(_filePath))
            return;

        var currentSection = "General";
        _sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in File.ReadAllLines(_filePath))
        {
            var trimmedLine = line.Trim();
            
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(';') || trimmedLine.StartsWith('#'))
                continue;

            if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'))
            {
                currentSection = trimmedLine[1..^1];
                if (!_sections.ContainsKey(currentSection))
                {
                    _sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
            }
            else
            {
                var separatorIndex = trimmedLine.IndexOf('=');
                if (separatorIndex > 0)
                {
                    var key = trimmedLine[..separatorIndex].Trim();
                    var value = trimmedLine[(separatorIndex + 1)..].Trim();
                    
                    // Remove quotes if present
                    if (value.StartsWith('"') && value.EndsWith('"'))
                        value = value[1..^1];
                    
                    _sections[currentSection][key] = value;
                }
            }
        }
    }

    public void Save()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var writer = new StreamWriter(_filePath);
        
        foreach (var section in _sections)
        {
            writer.WriteLine($"[{section.Key}]");
            
            foreach (var kvp in section.Value)
            {
                var value = kvp.Value.Contains(' ') ? $"\"{kvp.Value}\"" : kvp.Value;
                writer.WriteLine($"{kvp.Key}={value}");
            }
            
            writer.WriteLine();
        }
    }

    public string? GetValue(string section, string key, string? defaultValue = null)
    {
        if (_sections.TryGetValue(section, out var sectionDict))
        {
            if (sectionDict.TryGetValue(key, out var value))
            {
                return value;
            }
        }
        return defaultValue;
    }

    public void SetValue(string section, string key, string value)
    {
        if (!_sections.ContainsKey(section))
        {
            _sections[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        
        _sections[section][key] = value;
    }

    public int GetInt(string section, string key, int defaultValue = 0)
    {
        var value = GetValue(section, key);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    public bool GetBool(string section, string key, bool defaultValue = false)
    {
        var value = GetValue(section, key);
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    public void SetInt(string section, string key, int value)
        => SetValue(section, key, value.ToString());

    public void SetBool(string section, string key, bool value)
        => SetValue(section, key, value.ToString().ToLower());

    public bool HasSection(string section) => _sections.ContainsKey(section);

    public bool HasKey(string section, string key)
        => _sections.TryGetValue(section, out var dict) && dict.ContainsKey(key);

    public void RemoveSection(string section) => _sections.Remove(section);

    public void RemoveKey(string section, string key)
    {
        if (_sections.TryGetValue(section, out var dict))
        {
            dict.Remove(key);
        }
    }

    public IEnumerable<string> GetSections() => _sections.Keys;

    public IEnumerable<KeyValuePair<string, string>> GetSectionValues(string section)
        => _sections.TryGetValue(section, out var dict) ? dict : Enumerable.Empty<KeyValuePair<string, string>>();
}

/// <summary>
/// User settings stored in INI file
/// </summary>
public class UserSettings
{
    private readonly IniFile _iniFile;
    
    public UserSettings(string? configPath = null)
    {
        var path = configPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CryptoTrader",
            "settings.ini"
        );
        _iniFile = new IniFile(path);
    }

    // Connection settings
    public string ServerAddress
    {
        get => _iniFile.GetValue("Connection", "ServerAddress", "localhost") ?? "localhost";
        set => _iniFile.SetValue("Connection", "ServerAddress", value);
    }

    public int TcpPort
    {
        get => _iniFile.GetInt("Connection", "TcpPort", 5000);
        set => _iniFile.SetInt("Connection", "TcpPort", value);
    }

    public int UdpPort
    {
        get => _iniFile.GetInt("Connection", "UdpPort", 5001);
        set => _iniFile.SetInt("Connection", "UdpPort", value);
    }

    public int HttpPort
    {
        get => _iniFile.GetInt("Connection", "HttpPort", 5002);
        set => _iniFile.SetInt("Connection", "HttpPort", value);
    }

    // Authentication
    public bool RememberMe
    {
        get => _iniFile.GetBool("Auth", "RememberMe", false);
        set => _iniFile.SetBool("Auth", "RememberMe", value);
    }

    public string? SavedUsername
    {
        get => _iniFile.GetValue("Auth", "Username");
        set
        {
            if (value != null)
                _iniFile.SetValue("Auth", "Username", value);
            else
                _iniFile.RemoveKey("Auth", "Username");
        }
    }

    public string? SavedToken
    {
        get => _iniFile.GetValue("Auth", "Token");
        set
        {
            if (value != null)
                _iniFile.SetValue("Auth", "Token", value);
            else
                _iniFile.RemoveKey("Auth", "Token");
        }
    }

    // UI preferences
    public string Theme
    {
        get => _iniFile.GetValue("UI", "Theme", "Dark") ?? "Dark";
        set => _iniFile.SetValue("UI", "Theme", value);
    }

    public string Currency
    {
        get => _iniFile.GetValue("UI", "Currency", "USD") ?? "USD";
        set => _iniFile.SetValue("UI", "Currency", value);
    }

    public int RefreshIntervalSeconds
    {
        get => _iniFile.GetInt("UI", "RefreshInterval", 30);
        set => _iniFile.SetInt("UI", "RefreshInterval", value);
    }

    // Favorite coins
    public List<string> FavoriteCoins
    {
        get
        {
            var value = _iniFile.GetValue("Favorites", "Coins", "bitcoin,ethereum");
            return value?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
        }
        set => _iniFile.SetValue("Favorites", "Coins", string.Join(",", value));
    }

    public void Save() => _iniFile.Save();
    public void Load() => _iniFile.Load();

    public void ClearAuthData()
    {
        SavedUsername = null;
        SavedToken = null;
        RememberMe = false;
        Save();
    }
}
