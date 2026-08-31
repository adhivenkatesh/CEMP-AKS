using Microsoft.EntityFrameworkCore;

namespace Employee.Api.Data
{
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Department { get; set; }
        public string? Email { get; set; }
        public decimal Salary { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Employee> Employees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Employee>().ToTable("Employees");
        }
    }

    // Aliases - so any name you use will work
    public class EmployeeContext : AppDbContext
    {
        public EmployeeContext(DbContextOptions<AppDbContext> options) : base(options) { }
    }

    public class EmployeeDbContext : AppDbContext
    {
        public EmployeeDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    }
}