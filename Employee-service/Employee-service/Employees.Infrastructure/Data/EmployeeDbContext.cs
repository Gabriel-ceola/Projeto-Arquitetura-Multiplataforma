using Employee_service.Employees.Domain;
using Microsoft.EntityFrameworkCore;

namespace Employee_service.Employees.Infrastructure.Data;

/// <summary>
/// DbContext da camada de infraestrutura.
/// Configura o mapeamento da entidade Employee para o PostgreSQL.
/// </summary>
public class EmployeeDbContext : DbContext
{
    public EmployeeDbContext(DbContextOptions<EmployeeDbContext> options)
        : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("employees");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .IsRequired();

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Cpf)
                .HasColumnName("cpf")
                .HasMaxLength(11)
                .IsRequired();

            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(e => e.Phone)
                .HasColumnName("phone")
                .HasMaxLength(11)
                .IsRequired();

            entity.Property(e => e.Role)
                .HasColumnName("role")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            entity.Property(e => e.DeletedAt)
                .HasColumnName("deleted_at");

            // CPF único: garante no banco a mesma regra validada no caso de uso
            entity.HasIndex(e => e.Cpf)
                .IsUnique()
                .HasDatabaseName("ix_employees_cpf");
        });
    }
}
