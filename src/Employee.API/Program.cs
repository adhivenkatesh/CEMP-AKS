// Old code starts .......

//var builder = WebApplication.CreateBuilder(args);

//// Add services
//builder.Services.AddOpenApi();

//var health = "Running successfully with github-actions 27th Aug 2026 at 13:59 pm IST !.";

//var appName = Environment.GetEnvironmentVariable("APP_NAME");
//var version = Environment.GetEnvironmentVariable("APP_VERSION");
//var company = Environment.GetEnvironmentVariable("COMPANY_NAME");

//// SQL Database configuration
//var dbServer = Environment.GetEnvironmentVariable("DB_SERVER");
//var dbDatabase = Environment.GetEnvironmentVariable("DB_DATABASE");
//var dbUser = Environment.GetEnvironmentVariable("DB_USERNAME");
//var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

//var apiKey = Environment.GetEnvironmentVariable("API_KEY");

//var connectionString =
//    $"Server={dbServer};" +
//    $"Database={dbDatabase};" +
//    $"User Id={dbUser};" +
//    $"Password={dbPassword};" +
//    "TrustServerCertificate=True;";

//var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
//    app.MapOpenApi();
//}

//app.MapGet("/", () =>
//{
//    return Results.Ok(new
//    {
//        Message = $"Welcome!. to {appName}",
//        Version = version,
//        Company = company
//    });
//});


//app.MapGet("/api/config", () =>
//{
//    return Results.Ok(new
//    {
//        ApplicationName = appName,
//        Version = version,
//        Company = company,

//        DatabaseServer = dbServer,
//        DatabaseName = dbDatabase,
//        DatabaseUser = dbUser,

//        DatabaseConfigured = !string.IsNullOrEmpty(connectionString),

//        ApiKeyConfigured = !string.IsNullOrEmpty(apiKey)
//    });
//});

//app.MapGet("/api/health", () =>
//{
//    return Results.Ok(new
//    {
//        Health = health
//    });
//});
//app.Run("http://0.0.0.0:8080");

// Old code completes here....

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

var health = "Running successfully with github-actions as on:- " + $"{indianTime}" ;


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