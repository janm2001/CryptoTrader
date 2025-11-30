using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CryptoTrader.App.Services;

/// <summary>
/// Service for handling multi-language support (English/Croatian)
/// </summary>
public class LanguageService : INotifyPropertyChanged
{
    private static LanguageService? _instance;
    public static LanguageService Instance => _instance ??= new LanguageService();

    private string _currentLanguage = "en";
    
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly Dictionary<string, Dictionary<string, string>> _translations = new()
    {
        ["en"] = new Dictionary<string, string>
        {
            // General
            ["AppTitle"] = "CryptoTrader",
            ["Welcome"] = "Welcome",
            ["Loading"] = "Loading...",
            ["Save"] = "Save",
            ["Cancel"] = "Cancel",
            ["Delete"] = "Delete",
            ["Edit"] = "Edit",
            ["Add"] = "Add",
            ["Refresh"] = "Refresh",
            ["Search"] = "Search",
            ["Close"] = "Close",
            ["Yes"] = "Yes",
            ["No"] = "No",
            ["OK"] = "OK",
            ["Error"] = "Error",
            ["Success"] = "Success",
            ["Warning"] = "Warning",
            ["Confirm"] = "Confirm",
            
            // Navigation
            ["NavDashboard"] = "Dashboard",
            ["NavPortfolio"] = "Portfolio",
            ["NavMarket"] = "Market",
            ["NavTransactions"] = "Transactions",
            ["NavSettings"] = "Settings",
            ["NavAdmin"] = "Admin Panel",
            ["NavLogout"] = "Logout",
            
            // Login/Register
            ["Login"] = "Login",
            ["Register"] = "Register",
            ["Username"] = "Username",
            ["Password"] = "Password",
            ["ConfirmPassword"] = "Confirm Password",
            ["Email"] = "Email",
            ["RememberMe"] = "Remember Me",
            ["ForgotPassword"] = "Forgot Password?",
            ["NoAccount"] = "Don't have an account?",
            ["HasAccount"] = "Already have an account?",
            ["LoginSuccess"] = "Login successful!",
            ["LoginFailed"] = "Invalid username or password",
            ["RegisterSuccess"] = "Registration successful!",
            ["LogoutSuccess"] = "You have been logged out",
            
            // Dashboard
            ["TotalValue"] = "Total Portfolio Value",
            ["TotalProfit"] = "Total Profit/Loss",
            ["TodayChange"] = "Today's Change",
            ["TopGainers"] = "Top Gainers",
            ["TopLosers"] = "Top Losers",
            ["RecentTransactions"] = "Recent Transactions",
            ["MarketOverview"] = "Market Overview",
            
            // Portfolio
            ["MyHoldings"] = "My Holdings",
            ["AddHolding"] = "Add Holding",
            ["EditHolding"] = "Edit Holding",
            ["Coin"] = "Coin",
            ["Amount"] = "Amount",
            ["PurchasePrice"] = "Purchase Price",
            ["CurrentPrice"] = "Current Price",
            ["CurrentValue"] = "Current Value",
            ["ProfitLoss"] = "Profit/Loss",
            ["PurchaseDate"] = "Purchase Date",
            ["NoHoldings"] = "No holdings yet. Add your first cryptocurrency!",
            
            // Market
            ["CryptoMarket"] = "Cryptocurrency Market",
            ["Rank"] = "Rank",
            ["Name"] = "Name",
            ["Symbol"] = "Symbol",
            ["Price"] = "Price",
            ["Change24h"] = "24h Change",
            ["MarketCap"] = "Market Cap",
            ["Volume"] = "Volume (24h)",
            ["Supply"] = "Circulating Supply",
            ["LastUpdated"] = "Last Updated",
            ["SearchCrypto"] = "Search cryptocurrency...",
            
            // Transactions
            ["TransactionHistory"] = "Transaction History",
            ["AddTransaction"] = "Add Transaction",
            ["TransactionType"] = "Type",
            ["Buy"] = "Buy",
            ["Sell"] = "Sell",
            ["Date"] = "Date",
            ["PricePerUnit"] = "Price per Unit",
            ["TotalValue"] = "Total Value",
            ["Fee"] = "Fee",
            ["Notes"] = "Notes",
            ["NoTransactions"] = "No transactions yet.",
            
            // Settings
            ["Settings"] = "Settings",
            ["GeneralSettings"] = "General Settings",
            ["Language"] = "Language",
            ["Theme"] = "Theme",
            ["ThemeLight"] = "Light",
            ["ThemeDark"] = "Dark",
            ["ThemeSystem"] = "System Default",
            ["Currency"] = "Display Currency",
            ["ServerSettings"] = "Server Settings",
            ["ServerAddress"] = "Server Address",
            ["ServerPort"] = "Server Port",
            ["AutoConnect"] = "Auto Connect",
            ["NotificationSettings"] = "Notifications",
            ["PriceAlerts"] = "Price Alerts",
            ["EmailNotifications"] = "Email Notifications",
            ["SoundEnabled"] = "Sound Enabled",
            ["SaveSettings"] = "Save Settings",
            ["ResetDefaults"] = "Reset to Defaults",
            ["SettingsSaved"] = "Settings saved successfully!",
            
            // Admin
            ["AdminPanel"] = "Admin Panel",
            ["UserManagement"] = "User Management",
            ["AllUsers"] = "All Users",
            ["UserRole"] = "Role",
            ["UserStatus"] = "Status",
            ["Active"] = "Active",
            ["Inactive"] = "Inactive",
            ["Admin"] = "Admin",
            ["User"] = "User",
            ["ChangeRole"] = "Change Role",
            ["DeactivateUser"] = "Deactivate User",
            ["ActivateUser"] = "Activate User",
            ["SystemStats"] = "System Statistics",
            ["TotalUsers"] = "Total Users",
            ["ActiveUsers"] = "Active Users",
            ["TotalTransactions"] = "Total Transactions",
            ["RefreshPrices"] = "Refresh Prices",
            
            // Export
            ["Export"] = "Export",
            ["ExportExcel"] = "Export to Excel",
            ["ExportCSV"] = "Export to CSV",
            ["ExportPrices"] = "Export Prices",
            ["ExportHoldings"] = "Export Holdings",
            ["ExportTransactions"] = "Export Transactions",
            ["ExportReport"] = "Export Full Report",
            ["ExportSuccess"] = "Export completed successfully!",
            
            // Status/Connection
            ["Connected"] = "Connected",
            ["Disconnected"] = "Disconnected",
            ["Connecting"] = "Connecting...",
            ["ConnectionError"] = "Connection Error",
            ["ServerStatus"] = "Server Status",
            ["LastSync"] = "Last Sync",
            
            // Validation
            ["Required"] = "This field is required",
            ["InvalidEmail"] = "Invalid email address",
            ["PasswordTooShort"] = "Password must be at least 6 characters",
            ["PasswordsDontMatch"] = "Passwords do not match",
            ["UsernameTooShort"] = "Username must be at least 3 characters",
            ["InvalidAmount"] = "Please enter a valid amount",
        },
        
        ["hr"] = new Dictionary<string, string>
        {
            // General
            ["AppTitle"] = "CryptoTrader",
            ["Welcome"] = "Dobrodošli",
            ["Loading"] = "Učitavanje...",
            ["Save"] = "Spremi",
            ["Cancel"] = "Odustani",
            ["Delete"] = "Obriši",
            ["Edit"] = "Uredi",
            ["Add"] = "Dodaj",
            ["Refresh"] = "Osvježi",
            ["Search"] = "Pretraži",
            ["Close"] = "Zatvori",
            ["Yes"] = "Da",
            ["No"] = "Ne",
            ["OK"] = "U redu",
            ["Error"] = "Greška",
            ["Success"] = "Uspjeh",
            ["Warning"] = "Upozorenje",
            ["Confirm"] = "Potvrdi",
            
            // Navigation
            ["NavDashboard"] = "Nadzorna ploča",
            ["NavPortfolio"] = "Portfelj",
            ["NavMarket"] = "Tržište",
            ["NavTransactions"] = "Transakcije",
            ["NavSettings"] = "Postavke",
            ["NavAdmin"] = "Admin panel",
            ["NavLogout"] = "Odjava",
            
            // Login/Register
            ["Login"] = "Prijava",
            ["Register"] = "Registracija",
            ["Username"] = "Korisničko ime",
            ["Password"] = "Lozinka",
            ["ConfirmPassword"] = "Potvrdi lozinku",
            ["Email"] = "E-mail",
            ["RememberMe"] = "Zapamti me",
            ["ForgotPassword"] = "Zaboravili ste lozinku?",
            ["NoAccount"] = "Nemate račun?",
            ["HasAccount"] = "Već imate račun?",
            ["LoginSuccess"] = "Prijava uspješna!",
            ["LoginFailed"] = "Pogrešno korisničko ime ili lozinka",
            ["RegisterSuccess"] = "Registracija uspješna!",
            ["LogoutSuccess"] = "Uspješno ste odjavljeni",
            
            // Dashboard
            ["TotalValue"] = "Ukupna vrijednost portfelja",
            ["TotalProfit"] = "Ukupna dobit/gubitak",
            ["TodayChange"] = "Današnja promjena",
            ["TopGainers"] = "Najveći dobitnici",
            ["TopLosers"] = "Najveći gubitnici",
            ["RecentTransactions"] = "Nedavne transakcije",
            ["MarketOverview"] = "Pregled tržišta",
            
            // Portfolio
            ["MyHoldings"] = "Moja ulaganja",
            ["AddHolding"] = "Dodaj ulaganje",
            ["EditHolding"] = "Uredi ulaganje",
            ["Coin"] = "Kriptovaluta",
            ["Amount"] = "Količina",
            ["PurchasePrice"] = "Kupovna cijena",
            ["CurrentPrice"] = "Trenutna cijena",
            ["CurrentValue"] = "Trenutna vrijednost",
            ["ProfitLoss"] = "Dobit/Gubitak",
            ["PurchaseDate"] = "Datum kupnje",
            ["NoHoldings"] = "Još nemate ulaganja. Dodajte svoju prvu kriptovalutu!",
            
            // Market
            ["CryptoMarket"] = "Tržište kriptovaluta",
            ["Rank"] = "Rang",
            ["Name"] = "Naziv",
            ["Symbol"] = "Simbol",
            ["Price"] = "Cijena",
            ["Change24h"] = "Promjena 24h",
            ["MarketCap"] = "Tržišna kap.",
            ["Volume"] = "Volumen (24h)",
            ["Supply"] = "Opskrba u optjecaju",
            ["LastUpdated"] = "Zadnje ažurirano",
            ["SearchCrypto"] = "Pretraži kriptovalute...",
            
            // Transactions
            ["TransactionHistory"] = "Povijest transakcija",
            ["AddTransaction"] = "Dodaj transakciju",
            ["TransactionType"] = "Vrsta",
            ["Buy"] = "Kupnja",
            ["Sell"] = "Prodaja",
            ["Date"] = "Datum",
            ["PricePerUnit"] = "Cijena po jedinici",
            ["TotalValue"] = "Ukupna vrijednost",
            ["Fee"] = "Naknada",
            ["Notes"] = "Bilješke",
            ["NoTransactions"] = "Još nema transakcija.",
            
            // Settings
            ["Settings"] = "Postavke",
            ["GeneralSettings"] = "Opće postavke",
            ["Language"] = "Jezik",
            ["Theme"] = "Tema",
            ["ThemeLight"] = "Svijetla",
            ["ThemeDark"] = "Tamna",
            ["ThemeSystem"] = "Sustavna",
            ["Currency"] = "Valuta prikaza",
            ["ServerSettings"] = "Postavke poslužitelja",
            ["ServerAddress"] = "Adresa poslužitelja",
            ["ServerPort"] = "Port poslužitelja",
            ["AutoConnect"] = "Automatsko povezivanje",
            ["NotificationSettings"] = "Obavijesti",
            ["PriceAlerts"] = "Upozorenja o cijenama",
            ["EmailNotifications"] = "E-mail obavijesti",
            ["SoundEnabled"] = "Zvuk omogućen",
            ["SaveSettings"] = "Spremi postavke",
            ["ResetDefaults"] = "Vrati na zadano",
            ["SettingsSaved"] = "Postavke su uspješno spremljene!",
            
            // Admin
            ["AdminPanel"] = "Admin panel",
            ["UserManagement"] = "Upravljanje korisnicima",
            ["AllUsers"] = "Svi korisnici",
            ["UserRole"] = "Uloga",
            ["UserStatus"] = "Status",
            ["Active"] = "Aktivan",
            ["Inactive"] = "Neaktivan",
            ["Admin"] = "Administrator",
            ["User"] = "Korisnik",
            ["ChangeRole"] = "Promijeni ulogu",
            ["DeactivateUser"] = "Deaktiviraj korisnika",
            ["ActivateUser"] = "Aktiviraj korisnika",
            ["SystemStats"] = "Statistika sustava",
            ["TotalUsers"] = "Ukupno korisnika",
            ["ActiveUsers"] = "Aktivnih korisnika",
            ["TotalTransactions"] = "Ukupno transakcija",
            ["RefreshPrices"] = "Osvježi cijene",
            
            // Export
            ["Export"] = "Izvoz",
            ["ExportExcel"] = "Izvezi u Excel",
            ["ExportCSV"] = "Izvezi u CSV",
            ["ExportPrices"] = "Izvezi cijene",
            ["ExportHoldings"] = "Izvezi ulaganja",
            ["ExportTransactions"] = "Izvezi transakcije",
            ["ExportReport"] = "Izvezi puni izvještaj",
            ["ExportSuccess"] = "Izvoz uspješno završen!",
            
            // Status/Connection
            ["Connected"] = "Povezano",
            ["Disconnected"] = "Nije povezano",
            ["Connecting"] = "Povezivanje...",
            ["ConnectionError"] = "Greška povezivanja",
            ["ServerStatus"] = "Status poslužitelja",
            ["LastSync"] = "Zadnja sinkronizacija",
            
            // Validation
            ["Required"] = "Ovo polje je obavezno",
            ["InvalidEmail"] = "Neispravna e-mail adresa",
            ["PasswordTooShort"] = "Lozinka mora imati najmanje 6 znakova",
            ["PasswordsDontMatch"] = "Lozinke se ne podudaraju",
            ["UsernameTooShort"] = "Korisničko ime mora imati najmanje 3 znaka",
            ["InvalidAmount"] = "Unesite ispravnu količinu",
        }
    };

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value && _translations.ContainsKey(value))
            {
                _currentLanguage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEnglish));
                OnPropertyChanged(nameof(IsCroatian));
                LanguageChanged?.Invoke(this, value);
            }
        }
    }

    public bool IsEnglish => _currentLanguage == "en";
    public bool IsCroatian => _currentLanguage == "hr";

    public event EventHandler<string>? LanguageChanged;

    public string this[string key] => GetString(key);

    public string GetString(string key)
    {
        if (_translations.TryGetValue(_currentLanguage, out var langDict))
        {
            if (langDict.TryGetValue(key, out var value))
            {
                return value;
            }
        }
        
        // Fallback to English
        if (_translations.TryGetValue("en", out var enDict))
        {
            if (enDict.TryGetValue(key, out var value))
            {
                return value;
            }
        }
        
        return $"[{key}]";
    }

    public IEnumerable<string> AvailableLanguages => _translations.Keys;

    public string GetLanguageName(string code) => code switch
    {
        "en" => "English",
        "hr" => "Hrvatski",
        _ => code
    };

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
