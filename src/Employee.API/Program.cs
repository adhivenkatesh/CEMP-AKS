using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer; // Add this using directive to enable UseSqlServer extension method

//using em // <-- Add this, change Employee.API.Data to your folder
var builder = WebApplication.CreateBuilder(args);


builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Your existing helper - KEPT AS IS
string GetConfig(string upperKey, string lowerKey, string fallback = "")
{
    return builder.Configuration[upperKey]
        ?? builder.Configuration[lowerKey]
        ?? Environment.GetEnvironmentVariable(upperKey)
        ?? Environment.GetEnvironmentVariable(lowerKey)
        ?? Environment.GetEnvironmentVariable(upperKey.ToUpper())
        ?? fallback;
}

// ================= PERMANENT FIX - START =================
var dbServer = GetConfig("DB_SERVER", "DbServer", "mssql-service");
var dbDatabase = GetConfig("DB_DATABASE", "DbDatabase", "EmployeeDB");
var dbUser = GetConfig("DB_USER", "DbUser", "sa");
var dbPassword = GetConfig("DB_PASSWORD", "DbPassword", "YourStrong!Pass123");

// Try raw connection string from config
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

// FORCE SQL Password auth - kills SSPI / NetSecurityNative error
sqlBuilder.IntegratedSecurity = false;
sqlBuilder.TrustServerCertificate = true;
sqlBuilder.Encrypt = SqlConnectionEncryptOption.Optional;
sqlBuilder.Authentication = SqlAuthenticationMethod.SqlPassword;
sqlBuilder.MultipleActiveResultSets = true;
if (string.IsNullOrWhiteSpace(sqlBuilder.DataSource)) sqlBuilder.DataSource = $"{dbServer},1433";
if (string.IsNullOrWhiteSpace(sqlBuilder.InitialCatalog)) sqlBuilder.InitialCatalog = dbDatabase;
if (string.IsNullOrWhiteSpace(sqlBuilder.UserID)) sqlBuilder.UserID = dbUser;
if (string.IsNullOrWhiteSpace(sqlBuilder.Password)) sqlBuilder.Password = dbPassword;

string finalConnString = sqlBuilder.ConnectionString;

Console.WriteLine($"ENV={builder.Environment.EnvironmentName} | Server={sqlBuilder.DataSource} | DB={sqlBuilder.InitialCatalog} | Auth={(sqlBuilder.IntegratedSecurity ? "Windows-LOCAL" : "SqlPassword")}");
Console.WriteLine($"Conn:{sqlBuilder.DataSource}/{sqlBuilder.InitialCatalog} Env:{builder.Environment.EnvironmentName}");

// Register DbContext - CHANGE EmployeeDbContext to your actual context name if different
builder.Services.AddDbContext<DbContext>(options =>
    options.UseSqlServer(finalConnString));

builder.Services.AddSingleton(finalConnString); // use this for now

// ================= PERMANENT FIX - END =================

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Your existing time logic - KEPT AS IS
DateTime utcNow = DateTime.UtcNow;
TimeZoneInfo istZone;
try
{
    istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
}
catch
{
    istZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
}
DateTime istNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, istZone);

// Your existing root endpoints - KEPT
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