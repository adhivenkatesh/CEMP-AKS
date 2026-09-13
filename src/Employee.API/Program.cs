using Azure.Storage.Blobs;
using Azure.Storage;
using Employee.API.Employee.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var accountName = "devstoreaccount1";
var accountKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";
var blobUri = new Uri("http://azurite:10000/devstoreaccount1");
Console.WriteLine($"BLOB FORCED TO: {blobUri}");
builder.Services.AddSingleton(_ => {
    return new BlobServiceClient(blobUri, new StorageSharedKeyCredential(accountName, accountKey));
});

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("EmployeeDB_Local"));

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
    try
    {
        var blobService = scope.ServiceProvider.GetRequiredService<BlobServiceClient>();
        blobService.GetBlobContainerClient("employee-photos").CreateIfNotExists();
        blobService.GetBlobContainerClient("employee-photos-thumb").CreateIfNotExists();
        Console.WriteLine("BLOB CONTAINERS READY");
    }
    catch (Exception ex) { Console.WriteLine($"Blob init warning: {ex.Message}"); }
}

app.MapGet("/", () => new { message = "Welcome! to Employee.API", version = "v19-azurite-fixed", time = DateTime.Now, blob = "http://azurite:10000/devstoreaccount1" });
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


app.MapPost("/api/employees/{id}/photo", async (int id, HttpRequest request, BlobServiceClient blobService) =>
{
    if (!request.HasFormContentType) return Results.BadRequest("No file");
    var form = await request.ReadFormAsync();
    var file = form.Files["file"];
    if (file == null) return Results.BadRequest("file field missing");

    var container = blobService.GetBlobContainerClient("employee-photos");
    await container.CreateIfNotExistsAsync();
    var blobName = $"{id}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
    var client = container.GetBlobClient(blobName);

    using var stream = file.OpenReadStream();
    await client.UploadAsync(stream, new Azure.Storage.Blobs.Models.BlobHttpHeaders { ContentType = file.ContentType });

    return Results.Ok(new { message = "Uploaded", blobName });
});

app.Run();
