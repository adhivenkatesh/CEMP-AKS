
// *************  NEW CHANGES STARTS  **********************

var builder = WebApplication.CreateBuilder(args);

// FIX: Load ConfigMap + Secret + appsettings
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables(); // reads K8s envFrom

// Add services
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Helper to read env in any case - fixes your null
string GetConfig(string upperKey, string lowerKey, string fallback = "")
{
    return builder.Configuration[upperKey]
        ?? builder.Configuration[lowerKey]
        ?? Environment.GetEnvironmentVariable(upperKey)
        ?? Environment.GetEnvironmentVariable(lowerKey)
        ?? Environment.GetEnvironmentVariable(upperKey.ToUpper())
        ?? fallback;
}

DateTime utcNow = DateTime.UtcNow;

TimeZoneInfo istZone =  TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

DateTime indianTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, istZone);

var health = "Running successfully! with github-actions as on:- " + $"{indianTime}" ;


// Get env - if local Development use 5000, else AKS uses 80
var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
var port = Environment.GetEnvironmentVariable("API_PORT");

var appName = GetConfig("APP_NAME", "applicationName", "CEMP-Portal");
var version = GetConfig("APP_VERSION", "version", "v2.1.0");
var company = GetConfig("COMPANY_NAME", "company", "IKEA");

var dbServer = GetConfig("DB_SERVER", "databaseServer", "cemp-mssql");
var dbDatabase = GetConfig("DB_DATABASE", "databaseName", "CEMPDB");
var dbUser = GetConfig("DB_USERNAME", "databaseUser", "sa");
var dbPassword = GetConfig("DB_PASSWORD", "databasePassword");
var apiKey = GetConfig("API_KEY", "apiKey");


var connectionString =
    $"Server={dbServer};" +
    $"Database={dbDatabase};" +
    $"User Id={dbUser};" +
    $"Password={dbPassword};" +
    "TrustServerCertificate=True;";

app.MapGet("/", () =>
{
    return Results.Ok(new
    {
        Message = $"Welcome! to {appName}",
        Version = version,
        Company = company
    });
});

app.MapGet("api/check", () =>{
    return Results.Ok(new {Status="Running good!"});
});

app.MapGet("/api/config", () =>
{
    return Results.Ok(new
    {
        ApplicationName = appName,
        Version = version,
        Company = company,
        DatabaseConfigured = !string.IsNullOrEmpty(dbPassword) && dbPassword != "not-set",
        ApiKeyConfigured = !string.IsNullOrEmpty(apiKey),
        Status = "Running"
        // REMOVED: DatabaseServer, DatabaseName, DatabaseUser - never expose DB details
    });
});


app.MapGet("/api/health", () =>
{
    return Results.Ok(new { Health = health });
});

// REMOVED: /api/debug/env - never keep in prod, it leaks secrets

if (string.IsNullOrEmpty(port))
{
    port = env == "Development" ? "5000" : "80";
}

app.Run($"http://0.0.0.0:{port}");
// ENDS HERE 