using EmployeeManagement.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var employee = modelBuilder.Entity<Employee>();

        employee.ToTable("Employees");
        employee.HasKey(e => e.Id);

        employee.Property(e => e.FullName)
            .IsRequired()
            .HasMaxLength(200);

        employee.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(256);

        employee.Property(e => e.Department)
            .IsRequired()
            .HasMaxLength(100);

        employee.Property(e => e.Salary)
            .HasPrecision(18, 2);

        employee.Property(e => e.CreatedAt)
            .IsRequired();
    }
}
