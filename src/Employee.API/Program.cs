using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Employee.API.Employee.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var conn = cfg["Storage:Conn"] ?? Environment.GetEnvironmentVariable("Storage__Conn") ?? "UseDevelopmentStorage=true";

    // Local Azurite inside K8s
    if (conn.Contains("azurite") || conn.Contains("devstoreaccount1"))
    {
        return new BlobServiceClient(
            new Uri("http://azurite:10000/devstoreaccount1"),
            new Azure.Storage.StorageSharedKeyCredential(
                "devstoreaccount1",
                "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw=="));
    }
    return new BlobServiceClient(conn);
});
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("EmployeeDB_Local"));
}
else
{
    string server = Environment.GetEnvironmentVariable("DB_SERVER") ?? "mssql-service";
    if (!server.Contains(',') && !server.Contains(':')) server += ",1433";
    string db = Environment.GetEnvironmentVariable("DB_DATABASE") ?? Environment.GetEnvironmentVariable("DB_NAME") ?? "EmployeeDB";
    string user = Environment.GetEnvironmentVariable("DB_USER") ?? "sa";
    string pass = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "YourStrong!Pass123";
    string conn = $"Server={server};Database={db};User Id={user};Password={pass};TrustServerCertificate=True;Encrypt=Optional;MultipleActiveResultSets=true";
    builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(conn, sql => sql.EnableRetryOnFailure()));
}

var app = builder.Build();

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

app.MapGet("/", () => new { message = "Welcome! to Employee.API", version = "v17-with-photo", time = DateTime.Now });
app.MapGet("/api/employees", async (AppDbContext db) => await db.Employees.ToListAsync());
app.MapGet("/api/employees/{id:int}", async (AppDbContext db, int id) => await db.Employees.FindAsync(id) is EmployeeEntity e ? Results.Ok(e) : Results.NotFound());
app.MapPost("/api/employees", async (AppDbContext db, EmployeeEntity emp) => { db.Employees.Add(emp); await db.SaveChangesAsync(); return Results.Created($"/api/employees/{emp.Id}", emp); });
app.MapDelete("/api/employees/{id:int}", async (AppDbContext db, int id) => { var e = await db.Employees.FindAsync(id); if (e == null) return Results.NotFound(); db.Employees.Remove(e); await db.SaveChangesAsync(); return Results.Ok(); });


app.MapGet("/api/employees/{id}/photo", async (int id, BlobServiceClient blobService) =>
{
    var container = blobService.GetBlobContainerClient("employee-photos");
    await container.CreateIfNotExistsAsync();
    await foreach (var blob in container.GetBlobsAsync())
    {
        if (!blob.Name.StartsWith($"{id}_")) continue;
        var client = container.GetBlobClient(blob.Name);
        var download = await client.DownloadAsync();
        return Results.File(download.Value.Content, download.Value.ContentType);
    }
    return Results.NotFound("No photo yet");
});

app.MapGet("/api/employees/{id}/photo/{size}", async (int id, string size, BlobServiceClient blobService) =>
{
    var container = blobService.GetBlobContainerClient($"employee-photos-{size}");
    await container.CreateIfNotExistsAsync();
    await foreach (var blobItem in container.GetBlobsAsync())
    {
        if (!blobItem.Name.StartsWith($"{id}_")) continue;
        var client = container.GetBlobClient(blobItem.Name);
        var download = await client.DownloadAsync();
        return Results.File(download.Value.Content, download.Value.ContentType);
    }
    return Results.NotFound($"No {size} photo");
});

app.Run();