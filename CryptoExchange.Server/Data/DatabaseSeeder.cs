using System.Security.Cryptography;
using CryptoTrader.Shared.Models;

namespace CryptoExchange.Server.Data;

/// <summary>
/// Seeds the database with initial data for testing
/// </summary>
public class DatabaseSeeder
{
    private readonly DatabaseContext _db;

    public DatabaseSeeder(DatabaseContext db)
    {
        _db = db;
    }

    public async Task SeedAsync()
    {
        Console.WriteLine("[Seeder] Checking database for seed data...");

        // Check if admin user exists
        var adminUser = await _db.GetUserByUsernameAsync("admin");
        if (adminUser == null)
        {
            Console.WriteLine("[Seeder] Creating admin user...");
            await CreateAdminUserAsync();
        }

        // Check if test user exists
        var testUser = await _db.GetUserByUsernameAsync("testuser");
        if (testUser == null)
        {
            Console.WriteLine("[Seeder] Creating test user...");
            await CreateTestUserAsync();
        }

        // Seed some crypto price data
        var prices = await _db.GetAllPricesAsync();
        if (!prices.Any())
        {
            Console.WriteLine("[Seeder] Seeding crypto price data...");
            await SeedCryptoPricesAsync();
        }

        // Add sample holdings for test user
        testUser = await _db.GetUserByUsernameAsync("testuser");
        if (testUser != null)
        {
            var holdings = await _db.GetUserHoldingsAsync(testUser.Id);
            if (!holdings.Any())
            {
                Console.WriteLine("[Seeder] Adding sample holdings for test user...");
                await SeedUserHoldingsAsync(testUser.Id);
            }
        }

        Console.WriteLine("[Seeder] Database seeding complete!");
    }

    private static string GenerateSalt()
    {
        var saltBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    private static string HashPassword(string password, string salt)
    {
        return BCrypt.Net.BCrypt.HashPassword(password + salt, BCrypt.Net.BCrypt.GenerateSalt(12));
    }

    private async Task CreateAdminUserAsync()
    {
        var salt = GenerateSalt();
        var passwordHash = HashPassword("admin123", salt);

        var admin = new User
        {
            Username = "admin",
            Email = "admin@cryptotrader.local",
            PasswordHash = passwordHash,
            Salt = salt,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            Role = UserRole.Admin
        };

        await _db.CreateUserAsync(admin);
        Console.WriteLine("[Seeder] Admin user created: username='admin', password='admin123'");
    }

    private async Task CreateTestUserAsync()
    {
        var salt = GenerateSalt();
        var passwordHash = HashPassword("test123", salt);

        var user = new User
        {
            Username = "testuser",
            Email = "test@cryptotrader.local",
            PasswordHash = passwordHash,
            Salt = salt,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            Role = UserRole.User
        };

        await _db.CreateUserAsync(user);
        Console.WriteLine("[Seeder] Test user created: username='testuser', password='test123'");
    }

    private async Task SeedCryptoPricesAsync()
    {
        var cryptos = new List<CryptoCurrency>
        {
            new()
            {
                CoinId = "bitcoin",
                Symbol = "btc",
                Name = "Bitcoin",
                CurrentPrice = 97500.00m,
                MarketCap = 1930000000000m,
                MarketCapRank = 1,
                TotalVolume = 45000000000m,
                High24h = 98500.00m,
                Low24h = 96000.00m,
                PriceChange24h = 1500.00m,
                PriceChangePercentage24h = 1.56m,
                CirculatingSupply = 19800000m,
                TotalSupply = 21000000m,
                MaxSupply = 21000000m,
                ImageUrl = "https://assets.coingecko.com/coins/images/1/large/bitcoin.png",
                LastUpdated = DateTime.UtcNow
            },
            new()
            {
                CoinId = "ethereum",
                Symbol = "eth",
                Name = "Ethereum",
                CurrentPrice = 3650.00m,
                MarketCap = 440000000000m,
                MarketCapRank = 2,
                TotalVolume = 18000000000m,
                High24h = 3700.00m,
                Low24h = 3580.00m,
                PriceChange24h = 45.00m,
                PriceChangePercentage24h = 1.25m,
                CirculatingSupply = 120500000m,
                TotalSupply = null,
                MaxSupply = null,
                ImageUrl = "https://assets.coingecko.com/coins/images/279/large/ethereum.png",
                LastUpdated = DateTime.UtcNow
            },
            new()
            {
                CoinId = "tether",
                Symbol = "usdt",
                Name = "Tether",
                CurrentPrice = 1.00m,
                MarketCap = 140000000000m,
                MarketCapRank = 3,
                TotalVolume = 85000000000m,
                High24h = 1.001m,
                Low24h = 0.999m,
                PriceChange24h = 0.00m,
                PriceChangePercentage24h = 0.01m,
                CirculatingSupply = 140000000000m,
                TotalSupply = 140000000000m,
                MaxSupply = null,
                ImageUrl = "https://assets.coingecko.com/coins/images/325/large/Tether.png",
                LastUpdated = DateTime.UtcNow
            },
            new()
            {
                CoinId = "binancecoin",
                Symbol = "bnb",
                Name = "BNB",
                CurrentPrice = 645.00m,
                MarketCap = 94000000000m,
                MarketCapRank = 4,
                TotalVolume = 1800000000m,
                High24h = 655.00m,
                Low24h = 635.00m,
                PriceChange24h = 8.50m,
                PriceChangePercentage24h = 1.34m,
                CirculatingSupply = 145900000m,
                TotalSupply = 145900000m,
                MaxSupply = 200000000m,
                ImageUrl = "https://assets.coingecko.com/coins/images/825/large/bnb-icon2_2x.png",
                LastUpdated = DateTime.UtcNow
            },
            new()
            {
                CoinId = "solana",
                Symbol = "sol",
                Name = "Solana",
                CurrentPrice = 242.00m,
                MarketCap = 115000000000m,
                MarketCapRank = 5,
                TotalVolume = 4500000000m,
                High24h = 248.00m,
                Low24h = 235.00m,
                PriceChange24h = 5.00m,
                PriceChangePercentage24h = 2.11m,
                CirculatingSupply = 475000000m,
                TotalSupply = 590000000m,
                MaxSupply = null,
                ImageUrl = "https://assets.coingecko.com/coins/images/4128/large/solana.png",
                LastUpdated = DateTime.UtcNow
            },
            new()
            {
                CoinId = "ripple",
                Symbol = "xrp",
                Name = "XRP",
                CurrentPrice = 1.48m,
                MarketCap = 84000000000m,
                MarketCapRank = 6,
                TotalVolume = 12000000000m,
                High24h = 1.52m,
                Low24h = 1.42m,
                PriceChange24h = 0.05m,
                PriceChangePercentage24h = 3.50m,
                CirculatingSupply = 57000000000m,
                TotalSupply = 100000000000m,
                MaxSupply = 100000000000m,
                ImageUrl = "https://assets.coingecko.com/coins/images/44/large/xrp-symbol-white-128.png",
                LastUpdated = DateTime.UtcNow
            },
            new()
            {
                CoinId = "cardano",
                Symbol = "ada",
                Name = "Cardano",
                CurrentPrice = 1.05m,
                MarketCap = 37000000000m,
                MarketCapRank = 7,
                TotalVolume = 1200000000m,
                High24h = 1.08m,
                Low24h = 1.01m,
                PriceChange24h = 0.03m,
                PriceChangePercentage24h = 2.94m,
                CirculatingSupply = 35300000000m,
                TotalSupply = 45000000000m,
                MaxSupply = 45000000000m,
                ImageUrl = "https://assets.coingecko.com/coins/images/975/large/cardano.png",
                LastUpdated = DateTime.UtcNow
            },
            new()
            {
                CoinId = "dogecoin",
                Symbol = "doge",
                Name = "Dogecoin",
                CurrentPrice = 0.42m,
                MarketCap = 62000000000m,
                MarketCapRank = 8,
                TotalVolume = 5500000000m,
                High24h = 0.44m,
                Low24h = 0.40m,
                PriceChange24h = 0.015m,
                PriceChangePercentage24h = 3.70m,
                CirculatingSupply = 147000000000m,
                TotalSupply = null,
                MaxSupply = null,
                ImageUrl = "https://assets.coingecko.com/coins/images/5/large/dogecoin.png",
                LastUpdated = DateTime.UtcNow
            },
            new()
            {
                CoinId = "polkadot",
                Symbol = "dot",
                Name = "Polkadot",
                CurrentPrice = 9.85m,
                MarketCap = 15000000000m,
                MarketCapRank = 9,
                TotalVolume = 650000000m,
                High24h = 10.20m,
                Low24h = 9.50m,
                PriceChange24h = 0.25m,
                PriceChangePercentage24h = 2.60m,
                CirculatingSupply = 1520000000m,
                TotalSupply = 1520000000m,
                MaxSupply = null,
                ImageUrl = "https://assets.coingecko.com/coins/images/12171/large/polkadot.png",
                LastUpdated = DateTime.UtcNow
            },
            new()
            {
                CoinId = "avalanche-2",
                Symbol = "avax",
                Name = "Avalanche",
                CurrentPrice = 45.50m,
                MarketCap = 18500000000m,
                MarketCapRank = 10,
                TotalVolume = 850000000m,
                High24h = 47.00m,
                Low24h = 44.00m,
                PriceChange24h = 1.20m,
                PriceChangePercentage24h = 2.71m,
                CirculatingSupply = 407000000m,
                TotalSupply = 720000000m,
                MaxSupply = 720000000m,
                ImageUrl = "https://assets.coingecko.com/coins/images/12559/large/Avalanche_Circle_RedWhite_Trans.png",
                LastUpdated = DateTime.UtcNow
            }
        };

        foreach (var crypto in cryptos)
        {
            await _db.UpsertPriceAsync(crypto);
        }

        Console.WriteLine($"[Seeder] Seeded {cryptos.Count} cryptocurrencies");
    }

    private async Task SeedUserHoldingsAsync(int userId)
    {
        var holdings = new List<CryptoHolding>
        {
            new()
            {
                UserId = userId,
                CoinId = "bitcoin",
                Symbol = "BTC",
                Amount = 0.5m,
                PurchasePrice = 65000.00m,
                PurchaseDate = DateTime.UtcNow.AddDays(-30)
            },
            new()
            {
                UserId = userId,
                CoinId = "ethereum",
                Symbol = "ETH",
                Amount = 5.0m,
                PurchasePrice = 3200.00m,
                PurchaseDate = DateTime.UtcNow.AddDays(-45)
            },
            new()
            {
                UserId = userId,
                CoinId = "solana",
                Symbol = "SOL",
                Amount = 25.0m,
                PurchasePrice = 180.00m,
                PurchaseDate = DateTime.UtcNow.AddDays(-15)
            },
            new()
            {
                UserId = userId,
                CoinId = "cardano",
                Symbol = "ADA",
                Amount = 1000.0m,
                PurchasePrice = 0.85m,
                PurchaseDate = DateTime.UtcNow.AddDays(-60)
            }
        };

        foreach (var holding in holdings)
        {
            await _db.AddHoldingAsync(holding);
        }

        // Add some transactions
        var transactions = new List<CryptoTransaction>
        {
            new()
            {
                UserId = userId,
                CoinId = "bitcoin",
                Symbol = "BTC",
                Type = TransactionType.Buy,
                Amount = 0.5m,
                PricePerUnit = 65000.00m,
                TotalValue = 32500.00m,
                Fee = 25.00m,
                Timestamp = DateTime.UtcNow.AddDays(-30),
                Notes = "Initial BTC purchase"
            },
            new()
            {
                UserId = userId,
                CoinId = "ethereum",
                Symbol = "ETH",
                Type = TransactionType.Buy,
                Amount = 5.0m,
                PricePerUnit = 3200.00m,
                TotalValue = 16000.00m,
                Fee = 15.00m,
                Timestamp = DateTime.UtcNow.AddDays(-45),
                Notes = "ETH accumulation"
            },
            new()
            {
                UserId = userId,
                CoinId = "solana",
                Symbol = "SOL",
                Type = TransactionType.Buy,
                Amount = 25.0m,
                PricePerUnit = 180.00m,
                TotalValue = 4500.00m,
                Fee = 5.00m,
                Timestamp = DateTime.UtcNow.AddDays(-15),
                Notes = "SOL position"
            }
        };

        foreach (var tx in transactions)
        {
            await _db.AddTransactionAsync(tx);
        }

        Console.WriteLine($"[Seeder] Added {holdings.Count} holdings and {transactions.Count} transactions for test user");
    }
}
