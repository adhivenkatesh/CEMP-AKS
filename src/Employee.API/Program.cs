using Employee.Api.Data; // <-- THIS WAS MISSING - matches your yellow folder
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddOpenApi();
builder.Services.AddControllers();

string GetConfig(string upperKey, string lowerKey, string fallback = "")
{
    return builder.Configuration[upperKey]
        ?? builder.Configuration[lowerKey]
        ?? Environment.GetEnvironmentVariable(upperKey)
        ?? Environment.GetEnvironmentVariable(lowerKey)
        ?? Environment.GetEnvironmentVariable(upperKey.ToUpper())
        ?? fallback;
}

var dbServer = GetConfig("DB_SERVER", "DbServer", "mssql-service");
var dbDatabase = GetConfig("DB_DATABASE", "DbDatabase", "EmployeeDB");
var dbUser = GetConfig("DB_USER", "DbUser", "sa");
var dbPassword = GetConfig("DB_PASSWORD", "DbPassword", "YourStrong!Pass123");

var rawConn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

SqlConnectionStringBuilder sqlBuilder;
if (!string.IsNullOrWhiteSpace(rawConn))
{
    sqlBuilder = new SqlConnectionStringBuilder(rawConn);
}
else
{
    sqlBuilder = new SqlConnectionStringBuilder
    {
        DataSource = $"{dbServer},1433",
        InitialCatalog = dbDatabase,
        UserID = dbUser,
        Password = dbPassword
    };
}

sqlBuilder.IntegratedSecurity = false;
sqlBuilder.TrustServerCertificate = true;
sqlBuilder.Encrypt = SqlConnectionEncryptOption.Optional;
sqlBuilder.Authentication = SqlAuthenticationMethod.SqlPassword;
sqlBuilder.MultipleActiveResultSets = true;

string finalConnString = sqlBuilder.ConnectionString;
Console.WriteLine($"ENV={builder.Environment.EnvironmentName} | Server={sqlBuilder.DataSource} | DB={sqlBuilder.InitialCatalog} | Auth=SqlPassword");

// TODO: Replace AppDbContext with name you saw inside Employee.Api.Data folder
// If you saw EmployeeContext, use EmployeeContext
// If you saw AppDbContext, use AppDbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(finalConnString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

DateTime utcNow = DateTime.UtcNow;
TimeZoneInfo istZone;
try { istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
catch { istZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
DateTime istNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, istZone);

app.MapGet("/", () => new
{
    message = "Welcome! to Employee.API",
    version = "9.0.17",
    company = "MPC",
    timeUtc = utcNow,
    timeIST = istNow
});

app.MapControllers();
app.Run();