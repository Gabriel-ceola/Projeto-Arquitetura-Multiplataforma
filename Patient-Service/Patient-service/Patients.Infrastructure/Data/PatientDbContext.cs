using Patient_service.Patients.Domain;
using Microsoft.EntityFrameworkCore;

namespace Patient_service.Patients.Infrastructure.Data;

/// <summary>
/// DbContext da camada de infraestrutura.
/// Configura o mapeamento da entidade Patient para o PostgreSQL.
/// </summary>
public class PatientDbContext : DbContext
{
    public PatientDbContext(DbContextOptions<PatientDbContext> options)
        : base(options) { }

    public DbSet<Patient> Patients => Set<Patient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("patients");

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

            entity.Property(e => e.BirthDate)
                .HasColumnName("birth_date")
                .IsRequired();

            entity.Property(e => e.InsuranceName)
                .HasColumnName("insurance_name")
                .HasMaxLength(120);

            entity.Property(e => e.InsuranceCardNumber)
                .HasColumnName("insurance_card_number")
                .HasMaxLength(80);

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

            // Propriedade calculada, não persistida
            entity.Ignore(e => e.HasInsurance);

            // CPF único: garante no banco a mesma regra validada no caso de uso
            entity.HasIndex(e => e.Cpf)
                .IsUnique()
                .HasDatabaseName("ix_patients_cpf");
        });
    }
}
