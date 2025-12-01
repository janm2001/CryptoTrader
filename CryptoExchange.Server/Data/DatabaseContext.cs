using Microsoft.Data.Sqlite;
using Dapper;
using CryptoTrader.Shared.Models;

namespace CryptoExchange.Server.Data;

/// <summary>
/// SQLite database context for CryptoTrader
/// </summary>
public class DatabaseContext
{
    private readonly string _connectionString;

    public DatabaseContext(string databasePath = "cryptotrader.db")
    {
        _connectionString = $"Data Source={databasePath}";
        InitializeDatabase();
    }

    private SqliteConnection GetConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private void InitializeDatabase()
    {
        using var connection = GetConnection();
        
        // Users table
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                Email TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL,
                Salt TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                LastLoginAt TEXT,
                IsActive INTEGER NOT NULL DEFAULT 1,
                Role INTEGER NOT NULL DEFAULT 0,
                Balance REAL NOT NULL DEFAULT 10000
            )");
        
        // Add Balance column if it doesn't exist (for existing databases)
        try
        {
            connection.Execute("ALTER TABLE Users ADD COLUMN Balance REAL NOT NULL DEFAULT 10000");
        }
        catch { /* Column already exists */ };

        // Crypto prices cache table
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS CryptoPrices (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CoinId TEXT NOT NULL UNIQUE,
                Symbol TEXT NOT NULL,
                Name TEXT NOT NULL,
                CurrentPrice REAL NOT NULL,
                MarketCap REAL NOT NULL,
                MarketCapRank INTEGER,
                TotalVolume REAL,
                High24h REAL,
                Low24h REAL,
                PriceChange24h REAL,
                PriceChangePercentage24h REAL,
                CirculatingSupply REAL,
                TotalSupply REAL,
                MaxSupply REAL,
                ImageUrl TEXT,
                LastUpdated TEXT NOT NULL
            )");

        // Price history table
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS PriceHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CoinId TEXT NOT NULL,
                Price REAL NOT NULL,
                MarketCap REAL,
                Volume REAL,
                Timestamp TEXT NOT NULL,
                FOREIGN KEY (CoinId) REFERENCES CryptoPrices(CoinId)
            )");

        // User holdings table
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Holdings (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                CoinId TEXT NOT NULL,
                Symbol TEXT NOT NULL,
                Amount REAL NOT NULL,
                PurchasePrice REAL NOT NULL,
                PurchaseDate TEXT NOT NULL,
                FOREIGN KEY (UserId) REFERENCES Users(Id),
                FOREIGN KEY (CoinId) REFERENCES CryptoPrices(CoinId)
            )");

        // Transactions table
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Transactions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                CoinId TEXT NOT NULL,
                Symbol TEXT NOT NULL,
                Type INTEGER NOT NULL,
                Amount REAL NOT NULL,
                PricePerUnit REAL NOT NULL,
                TotalValue REAL NOT NULL,
                Fee REAL NOT NULL DEFAULT 0,
                Timestamp TEXT NOT NULL,
                Notes TEXT,
                FOREIGN KEY (UserId) REFERENCES Users(Id)
            )");

        // User sessions table
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Sessions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Token TEXT NOT NULL UNIQUE,
                ExpiresAt TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY (UserId) REFERENCES Users(Id)
            )");

        // Create indexes
        connection.Execute("CREATE INDEX IF NOT EXISTS idx_pricehistory_coinid ON PriceHistory(CoinId)");
        connection.Execute("CREATE INDEX IF NOT EXISTS idx_holdings_userid ON Holdings(UserId)");
        connection.Execute("CREATE INDEX IF NOT EXISTS idx_transactions_userid ON Transactions(UserId)");
        connection.Execute("CREATE INDEX IF NOT EXISTS idx_sessions_token ON Sessions(Token)");
    }

    #region User Operations

    public async Task<User?> GetUserByIdAsync(int id)
    {
        using var connection = GetConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Id = @Id", new { Id = id });
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        using var connection = GetConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Username = @Username", new { Username = username });
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        using var connection = GetConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Email = @Email", new { Email = email });
    }

    public async Task<int> CreateUserAsync(User user)
    {
        using var connection = GetConnection();
        return await connection.ExecuteScalarAsync<int>(@"
            INSERT INTO Users (Username, Email, PasswordHash, Salt, CreatedAt, IsActive, Role, Balance)
            VALUES (@Username, @Email, @PasswordHash, @Salt, @CreatedAt, @IsActive, @Role, @Balance);
            SELECT last_insert_rowid();", user);
    }

    public async Task UpdateUserLastLoginAsync(int userId)
    {
        using var connection = GetConnection();
        await connection.ExecuteAsync(
            "UPDATE Users SET LastLoginAt = @LastLoginAt WHERE Id = @Id",
            new { Id = userId, LastLoginAt = DateTime.UtcNow.ToString("O") });
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        using var connection = GetConnection();
        return await connection.QueryAsync<User>("SELECT * FROM Users ORDER BY CreatedAt DESC");
    }

    public async Task UpdateUserRoleAsync(int userId, UserRole role)
    {
        using var connection = GetConnection();
        await connection.ExecuteAsync(
            "UPDATE Users SET Role = @Role WHERE Id = @Id",
            new { Id = userId, Role = role });
    }

    public async Task SetUserActiveStatusAsync(int userId, bool isActive)
    {
        using var connection = GetConnection();
        await connection.ExecuteAsync(
            "UPDATE Users SET IsActive = @IsActive WHERE Id = @Id",
            new { Id = userId, IsActive = isActive ? 1 : 0 });
    }

    public async Task<decimal> GetUserBalanceAsync(int userId)
    {
        using var connection = GetConnection();
        return await connection.ExecuteScalarAsync<decimal>(
            "SELECT Balance FROM Users WHERE Id = @Id", new { Id = userId });
    }

    public async Task UpdateUserBalanceAsync(int userId, decimal newBalance)
    {
        using var connection = GetConnection();
        await connection.ExecuteAsync(
            "UPDATE Users SET Balance = @Balance WHERE Id = @Id",
            new { Id = userId, Balance = newBalance });
    }

    public async Task<bool> DeductBalanceAsync(int userId, decimal amount)
    {
        using var connection = GetConnection();
        var currentBalance = await GetUserBalanceAsync(userId);
        if (currentBalance < amount) return false;
        
        await connection.ExecuteAsync(
            "UPDATE Users SET Balance = Balance - @Amount WHERE Id = @Id",
            new { Id = userId, Amount = amount });
        return true;
    }

    public async Task AddToBalanceAsync(int userId, decimal amount)
    {
        using var connection = GetConnection();
        await connection.ExecuteAsync(
            "UPDATE Users SET Balance = Balance + @Amount WHERE Id = @Id",
            new { Id = userId, Amount = amount });
    }

    #endregion

    #region Session Operations

    public async Task<string> CreateSessionAsync(int userId, int expirationHours = 24)
    {
        using var connection = GetConnection();
        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.AddHours(expirationHours);

        await connection.ExecuteAsync(@"
            INSERT INTO Sessions (UserId, Token, ExpiresAt, CreatedAt)
            VALUES (@UserId, @Token, @ExpiresAt, @CreatedAt)",
            new { UserId = userId, Token = token, ExpiresAt = expiresAt.ToString("O"), CreatedAt = DateTime.UtcNow.ToString("O") });

        return token;
    }

    public async Task<UserSession?> ValidateSessionAsync(string token)
    {
        using var connection = GetConnection();
        var result = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT s.UserId, u.Username, s.Token, s.ExpiresAt
            FROM Sessions s
            JOIN Users u ON s.UserId = u.Id
            WHERE s.Token = @Token", new { Token = token });

        if (result == null) return null;

        var expiresAt = DateTime.Parse((string)result.ExpiresAt);
        if (DateTime.UtcNow >= expiresAt)
        {
            await DeleteSessionAsync(token);
            return null;
        }

        return new UserSession
        {
            UserId = (int)(long)result.UserId,
            Username = (string)result.Username,
            Token = (string)result.Token,
            ExpiresAt = expiresAt
        };
    }

    public async Task DeleteSessionAsync(string token)
    {
        using var connection = GetConnection();
        await connection.ExecuteAsync("DELETE FROM Sessions WHERE Token = @Token", new { Token = token });
    }

    public async Task DeleteUserSessionsAsync(int userId)
    {
        using var connection = GetConnection();
        await connection.ExecuteAsync("DELETE FROM Sessions WHERE UserId = @UserId", new { UserId = userId });
    }

    #endregion

    #region Crypto Price Operations

    public async Task<IEnumerable<CryptoCurrency>> GetAllPricesAsync()
    {
        using var connection = GetConnection();
        return await connection.QueryAsync<CryptoCurrency>("SELECT * FROM CryptoPrices ORDER BY MarketCapRank");
    }

    public async Task<CryptoCurrency?> GetPriceAsync(string coinId)
    {
        using var connection = GetConnection();
        return await connection.QueryFirstOrDefaultAsync<CryptoCurrency>(
            "SELECT * FROM CryptoPrices WHERE CoinId = @CoinId", new { CoinId = coinId });
    }

    public async Task UpsertPriceAsync(CryptoCurrency crypto)
    {
        using var connection = GetConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO CryptoPrices (CoinId, Symbol, Name, CurrentPrice, MarketCap, MarketCapRank, 
                TotalVolume, High24h, Low24h, PriceChange24h, PriceChangePercentage24h,
                CirculatingSupply, TotalSupply, MaxSupply, ImageUrl, LastUpdated)
            VALUES (@CoinId, @Symbol, @Name, @CurrentPrice, @MarketCap, @MarketCapRank,
                @TotalVolume, @High24h, @Low24h, @PriceChange24h, @PriceChangePercentage24h,
                @CirculatingSupply, @TotalSupply, @MaxSupply, @ImageUrl, @LastUpdated)
            ON CONFLICT(CoinId) DO UPDATE SET
                CurrentPrice = @CurrentPrice, MarketCap = @MarketCap, MarketCapRank = @MarketCapRank,
                TotalVolume = @TotalVolume, High24h = @High24h, Low24h = @Low24h,
                PriceChange24h = @PriceChange24h, PriceChangePercentage24h = @PriceChangePercentage24h,
                CirculatingSupply = @CirculatingSupply, TotalSupply = @TotalSupply, MaxSupply = @MaxSupply,
                ImageUrl = @ImageUrl, LastUpdated = @LastUpdated",
            new
            {
                crypto.CoinId,
                crypto.Symbol,
                crypto.Name,
                crypto.CurrentPrice,
                crypto.MarketCap,
                crypto.MarketCapRank,
                crypto.TotalVolume,
                crypto.High24h,
                crypto.Low24h,
                crypto.PriceChange24h,
                crypto.PriceChangePercentage24h,
                crypto.CirculatingSupply,
                crypto.TotalSupply,
                crypto.MaxSupply,
                crypto.ImageUrl,
                LastUpdated = crypto.LastUpdated.ToString("O")
            });
    }

    public async Task AddPriceHistoryAsync(string coinId, decimal price, decimal marketCap, decimal volume)
    {
        using var connection = GetConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO PriceHistory (CoinId, Price, MarketCap, Volume, Timestamp)
            VALUES (@CoinId, @Price, @MarketCap, @Volume, @Timestamp)",
            new { CoinId = coinId, Price = price, MarketCap = marketCap, Volume = volume, Timestamp = DateTime.UtcNow.ToString("O") });
    }

    #endregion

    #region Holdings Operations

    public async Task<IEnumerable<CryptoHolding>> GetUserHoldingsAsync(int userId)
    {
        using var connection = GetConnection();
        return await connection.QueryAsync<CryptoHolding>(
            "SELECT * FROM Holdings WHERE UserId = @UserId", new { UserId = userId });
    }

    public async Task<int> AddHoldingAsync(CryptoHolding holding)
    {
        using var connection = GetConnection();
        return await connection.ExecuteScalarAsync<int>(@"
            INSERT INTO Holdings (UserId, CoinId, Symbol, Amount, PurchasePrice, PurchaseDate)
            VALUES (@UserId, @CoinId, @Symbol, @Amount, @PurchasePrice, @PurchaseDate);
            SELECT last_insert_rowid();",
            new
            {
                holding.UserId,
                holding.CoinId,
                holding.Symbol,
                holding.Amount,
                holding.PurchasePrice,
                PurchaseDate = holding.PurchaseDate.ToString("O")
            });
    }

    public async Task UpdateHoldingAsync(CryptoHolding holding)
    {
        using var connection = GetConnection();
        await connection.ExecuteAsync(@"
            UPDATE Holdings SET Amount = @Amount, PurchasePrice = @PurchasePrice
            WHERE Id = @Id AND UserId = @UserId",
            new { holding.Id, holding.UserId, holding.Amount, holding.PurchasePrice });
    }

    public async Task DeleteHoldingAsync(int holdingId, int userId)
    {
        using var connection = GetConnection();
        await connection.ExecuteAsync(
            "DELETE FROM Holdings WHERE Id = @Id AND UserId = @UserId",
            new { Id = holdingId, UserId = userId });
    }

    #endregion

    #region Transaction Operations

    public async Task<IEnumerable<CryptoTransaction>> GetUserTransactionsAsync(int userId, int limit = 100)
    {
        using var connection = GetConnection();
        return await connection.QueryAsync<CryptoTransaction>(
            "SELECT * FROM Transactions WHERE UserId = @UserId ORDER BY Timestamp DESC LIMIT @Limit",
            new { UserId = userId, Limit = limit });
    }

    public async Task<int> AddTransactionAsync(CryptoTransaction transaction)
    {
        using var connection = GetConnection();
        return await connection.ExecuteScalarAsync<int>(@"
            INSERT INTO Transactions (UserId, CoinId, Symbol, Type, Amount, PricePerUnit, TotalValue, Fee, Timestamp, Notes)
            VALUES (@UserId, @CoinId, @Symbol, @Type, @Amount, @PricePerUnit, @TotalValue, @Fee, @Timestamp, @Notes);
            SELECT last_insert_rowid();",
            new
            {
                transaction.UserId,
                transaction.CoinId,
                transaction.Symbol,
                transaction.Type,
                transaction.Amount,
                transaction.PricePerUnit,
                transaction.TotalValue,
                transaction.Fee,
                Timestamp = transaction.Timestamp.ToString("O"),
                transaction.Notes
            });
    }

    #endregion
}
