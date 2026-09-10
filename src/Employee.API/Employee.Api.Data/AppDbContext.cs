using Microsoft.EntityFrameworkCore;
namespace Employee.API.Employee.Api.Data
{
    public class EmployeeEntity { public int Id { get; set; } public string Name { get; set; } = ""; public string? Department { get; set; } public string? Email { get; set; } public decimal Salary { get; set; } }
    public class AppDbContext : DbContext { public AppDbContext(DbContextOptions<AppDbContext> o) : base(o) { } public DbSet<EmployeeEntity> Employees { get; set; } }
}