# CryptoTrader

A cryptocurrency tracking application built with C# .NET 9, featuring:

- **Avalonia UI** - Cross-platform desktop client
- **REST API** - HTTP endpoints for crypto data
- **TCP Server** - Real-time communication
- **UDP Server** - Low-latency price broadcasts
- **SQLite** - Local data persistence
- **INI Settings** - User preferences and session storage

## Project Structure

```
CryptoTrader/
├── CryptoTrader.Shared/     # Shared models and utilities
│   ├── Models/              # Data models (CryptoCurrency, User, etc.)
│   └── Config/              # INI file handling and settings
├── CryptoExchange.Server/   # Backend server
│   ├── Controllers/         # REST API endpoints
│   ├── Services/            # Business logic and servers
│   └── Data/                # Database context
└── CryptoTrader.App/        # Avalonia desktop client
    ├── Services/            # API client
    └── ViewModels/          # MVVM view models
```

## Quick Start

### Prerequisites

- .NET 9 SDK
- macOS, Windows, or Linux

### Running Both Projects Together

**macOS/Linux:**

```bash
chmod +x start.sh
./start.sh
```

**Windows:**

```cmd
start.bat
```

### Running Separately

**Start the Server:**

```bash
cd CryptoExchange.Server
dotnet run
```

The server will start:

- REST API at `http://localhost:5002`
- TCP Server on port `5000`
- UDP Server on port `5001`
- Swagger UI at `http://localhost:5002/swagger`

**Start the Client:**

```bash
cd CryptoTrader.App
dotnet run
```

## Features

### Server Features

- **REST API** - Full CRUD operations for crypto data

  - `GET /api/crypto/top` - Top cryptocurrencies by market cap
  - `GET /api/crypto/{coinId}` - Get specific coin details
  - `GET /api/crypto/search?query=` - Search cryptocurrencies
  - `POST /api/auth/register` - User registration
  - `POST /api/auth/login` - User login
  - `GET /api/portfolio/holdings` - User's holdings
  - `GET /api/portfolio/summary` - Portfolio summary

- **TCP Server** - Persistent connections for authenticated clients
- **UDP Server** - Broadcast price updates to subscribers
- **Background Service** - Fetches prices from CoinGecko every 30 seconds

### Client Features

- User registration and login
- Remember me functionality (stored in INI file)
- Real-time crypto price display
- Search functionality
- Clean, modern dark theme UI

## Configuration

### Server Configuration

Server settings are defined in `CryptoTrader.Shared/Config/ServerConfig.cs`:

- TCP Port: 5000
- UDP Port: 5001
- HTTP Port: 5002
- Database: `cryptotrader.db`
- Price update interval: 30 seconds

### Client Settings

User settings are stored in an INI file at:

- **macOS/Linux:** `~/.config/CryptoTrader/settings.ini`
- **Windows:** `%APPDATA%\CryptoTrader\settings.ini`

Settings include:

- Server connection details
- Saved credentials (when "Remember Me" is checked)
- UI preferences (theme, currency)
- Favorite coins

## API Documentation

When the server is running, visit `http://localhost:5002/swagger` for interactive API documentation.

## Database

SQLite database (`cryptotrader.db`) is created automatically with tables:

- `Users` - User accounts
- `Sessions` - Authentication sessions
- `CryptoPrices` - Cached cryptocurrency data
- `PriceHistory` - Historical price data
- `Holdings` - User portfolio holdings
- `Transactions` - Trading history

## Technologies Used

- **.NET 9** - Core framework
- **Avalonia UI 11** - Cross-platform UI framework
- **ASP.NET Core** - REST API framework
- **SQLite + Dapper** - Database
- **CoinGecko API** - Cryptocurrency data source
- **BCrypt** - Password hashing
- **CommunityToolkit.Mvvm** - MVVM framework

## License

MIT License
