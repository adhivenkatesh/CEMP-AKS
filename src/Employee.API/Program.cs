using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddOpenApi();
builder.Services.AddControllers();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

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
TimeZoneInfo istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
DateTime indianTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, istZone);
var health = "Running successfully! with github-actions as on:- " + $"{indianTime}";

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
var port = Environment.GetEnvironmentVariable("API_PORT");
var appName = GetConfig("APP_NAME", "applicationName", "CEMP-Portal");
var version = GetConfig("APP_VERSION", "version", "v2.1.0");
var company = GetConfig("COMPANY_NAME", "company", "IKEA");
var dbServer = GetConfig("DB_SERVER", "databaseServer", "mssql-service");
var dbDatabase = GetConfig("DB_DATABASE", "databaseName", "EmployeeDB");
var dbUser = GetConfig("DB_USERNAME", "databaseUser", "sa");
var dbPassword = GetConfig("DB_PASSWORD", "databasePassword", "YourStrong!Pass123");
var apiKey = GetConfig("API_KEY", "apiKey");

var connectionString =
    $"Server={dbServer};" +
    $"Database={dbDatabase};" +
    $"User Id={dbUser};" +
    $"Password={dbPassword};" +
    "TrustServerCertificate=True;";

// YOUR EXISTING METHODS - KEPT AS IS
app.MapGet("/", () => Results.Ok(new { Message = $"Welcome! to {appName}", Version = version, Company = company }));
app.MapGet("api/check", () => Results.Ok(new { Status = "Running good!" }));
app.MapGet("api/welcome", () => Results.Ok(new { Status = "Welcome to this session!" }));
app.MapGet("/api/config", () => Results.Ok(new { ApplicationName = appName, Version = version, Company = company, DatabaseConfigured = !string.IsNullOrEmpty(dbPassword) && dbPassword != "not-set", ApiKeyConfigured = !string.IsNullOrEmpty(apiKey), Status = "Running" }));
app.MapGet("/api/health", () => Results.Ok(new { Health = health }));

// NEW: ADD EMPLOYEES METHOD
app.MapGet("/api/employees", async () => {
    try
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        using var cmd = new SqlCommand("SELECT Id, Name, Email FROM Employees", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<object>();
        while (await reader.ReadAsync())
        {
            list.Add(new { Id = reader.GetInt32(0), Name = reader.GetString(1), Email = reader.GetString(2) });
        }
        return Results.Ok(list);
    }
    catch (Exception ex) { return Results.Problem(ex.Message + " Conn:" + dbServer + "/" + dbDatabase); }
});

app.MapPost("/api/employees", async (Employee emp) => {
    using var conn = new SqlConnection(connectionString);
    await conn.OpenAsync();
    using var cmd = new SqlCommand("INSERT INTO Employees (Name, Email) VALUES (@n, @e); SELECT SCOPE_IDENTITY()", conn);
    cmd.Parameters.AddWithValue("@n", emp.Name);
    cmd.Parameters.AddWithValue("@e", emp.Email);
    var id = await cmd.ExecuteScalarAsync();
    return Results.Ok(new { Id = id, emp.Name, emp.Email });
});

if (string.IsNullOrEmpty(port)) port = env == "Development" ? "5000" : "80";
app.Run($"http://0.0.0.0:{port}");

record Employee(string Name, string Email);