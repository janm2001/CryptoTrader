using CryptoExchange.Server.Data;
using CryptoExchange.Server.Services;
using CryptoTrader.Shared.Config;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Load server configuration
var config = new ServerConfig();

// Register services
var db = new DatabaseContext(config.DatabasePath);
builder.Services.AddSingleton(db);

// Run database seeder
var seeder = new DatabaseSeeder(db);
await seeder.SeedAsync();

var authService = new AuthService(db, config.TokenExpirationHours);
builder.Services.AddSingleton(authService);

var cryptoApiService = new CryptoApiService(db, config.CryptoApiBaseUrl);
builder.Services.AddSingleton(cryptoApiService);

var tcpServer = new TcpServerService(config.TcpPort, db, authService, cryptoApiService);
builder.Services.AddSingleton(tcpServer);

var udpServer = new UdpServerService(config.UdpPort);
builder.Services.AddSingleton(udpServer);

var priceUpdateService = new PriceUpdateService(cryptoApiService, tcpServer, udpServer, config.PriceUpdateIntervalMs);
builder.Services.AddSingleton(priceUpdateService);

// Add controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CryptoTrader API",
        Version = "v1",
        Description = "REST API for CryptoTrader - Cryptocurrency tracking and portfolio management"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter your token below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline - Always enable Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CryptoTrader API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Log events
tcpServer.OnLog += (s, msg) => Console.WriteLine(msg);
udpServer.OnLog += (s, msg) => Console.WriteLine(msg);
priceUpdateService.OnLog += (s, msg) => Console.WriteLine(msg);

// Start background services
var cts = new CancellationTokenSource();

Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

_ = Task.Run(() => tcpServer.StartAsync(cts.Token));
_ = Task.Run(() => udpServer.StartAsync(cts.Token));
_ = Task.Run(() => priceUpdateService.StartAsync(cts.Token));

Console.WriteLine("===========================================");
Console.WriteLine("   CryptoTrader Server Starting...        ");
Console.WriteLine("===========================================");
Console.WriteLine($"  HTTP/REST API: http://localhost:{config.HttpPort}");
Console.WriteLine($"  TCP Server:    port {config.TcpPort}");
Console.WriteLine($"  UDP Server:    port {config.UdpPort}");
Console.WriteLine($"  Swagger UI:    http://localhost:{config.HttpPort}/swagger");
Console.WriteLine("===========================================");
Console.WriteLine("Press Ctrl+C to stop the server");
Console.WriteLine();

try
{
    app.Run($"http://0.0.0.0:{config.HttpPort}");
}
finally
{
    cts.Cancel();
    tcpServer.Stop();
    udpServer.Stop();
    priceUpdateService.Stop();
}
