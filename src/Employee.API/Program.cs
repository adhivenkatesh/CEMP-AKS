using Employee.API.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

if (builder.Environment.IsDevelopment())
{
    // LOCAL VS - uses RAM, no SQL needed
    builder.Services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("EmployeeDB_Local"));
}
else
{
    // AKS - uses real MSSQL
    string server = Environment.GetEnvironmentVariable("DB_SERVER") ?? "mssql-service,1433";
    if (!server.Contains(',')) server += ",1433";
    string db = Environment.GetEnvironmentVariable("DB_DATABASE") ?? "EmployeeDB";
    string user = Environment.GetEnvironmentVariable("DB_USER") ?? "sa";
    string pass = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "YourStrong!Pass123";
    string conn = $"Server={server};Database={db};User Id={user};Password={pass};TrustServerCertificate=True;Encrypt=Optional;MultipleActiveResultSets=true";
    builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(conn, sql => sql.EnableRetryOnFailure()));
}

var app = builder.Build();

// Seed for both local and AKS
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    if (!db.Employees.Any())
    {
        db.Employees.Add(new EmployeeEntity { Name = "Adhi", Department = "DevOps", Email = "adhi@mpc.com", Salary = 90000 });
        db.SaveChanges();
    }
}

app.MapGet("/", () => new { message = "Welcome! to Employee.API", version = "16-final-dual-db", time = DateTime.Now });
app.MapGet("/api/employees", async (AppDbContext db) => await db.Employees.ToListAsync());
app.MapGet("/api/employees/{id:int}", async (AppDbContext db, int id) => await db.Employees.FindAsync(id) is EmployeeEntity e ? Results.Ok(e) : Results.NotFound());
app.MapPost("/api/employees", async (AppDbContext db, EmployeeEntity emp) => { db.Employees.Add(emp); await db.SaveChangesAsync(); return Results.Created($"/api/employees/{emp.Id}", emp); });
app.MapDelete("/api/employees/{id:int}", async (AppDbContext db, int id) => { var e = await db.Employees.FindAsync(id); if (e == null) return Results.NotFound(); db.Employees.Remove(e); await db.SaveChangesAsync(); return Results.Ok(); });

app.Run();