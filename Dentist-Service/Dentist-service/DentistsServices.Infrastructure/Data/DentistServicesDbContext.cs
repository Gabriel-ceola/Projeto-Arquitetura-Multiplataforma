using Dentist_service.DentistsServices.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dentist_service.DentistsServices.Infrastructure.Data;

/// <summary>
/// DbContext da camada de infraestrutura.
/// Configura o mapeamento da entidade DentistServices para o PostgreSQL.
/// </summary>
public class DentistServicesDbContext : DbContext
{
    public DentistServicesDbContext(DbContextOptions<DentistServicesDbContext> options)
        : base(options) { }

    public DbSet<DentistServices> DentistServices => Set<DentistServices>();
    public DbSet<DentistWeeklyHours> WeeklyHours => Set<DentistWeeklyHours>();
    public DbSet<DentistScheduleException> ScheduleExceptions => Set<DentistScheduleException>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DentistServices>(entity =>
        {
            entity.ToTable("dentist_services");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .IsRequired();

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.DentistId)
                .HasColumnName("dentist_id")
                .IsRequired();

            entity.Property(e => e.Price)
                .HasColumnName("price")
                .IsRequired();

            entity.Property(e => e.DurationMinutes)
                .HasColumnName("duration_minutes")
                .HasDefaultValue(30)
                .IsRequired();

            entity.Property(e => e.Isperiodic)
                .HasColumnName("is_periodic")
                .IsRequired();

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            entity.Property(e => e.DeletedAt)
                .HasColumnName("deleted_at");

            // Índice por dentista para consultas frequentes
            entity.HasIndex(e => e.DentistId)
                .HasDatabaseName("ix_dentist_services_dentist_id");
        });

        modelBuilder.Entity<DentistWeeklyHours>(entity =>
        {
            entity.ToTable("dentist_weekly_hours");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.DentistId).HasColumnName("dentist_id");
            entity.Property(x => x.DayOfWeek).HasColumnName("day_of_week");
            entity.Property(x => x.StartTime).HasColumnName("start_time");
            entity.Property(x => x.EndTime).HasColumnName("end_time");
            entity.HasIndex(x => new { x.DentistId, x.DayOfWeek }).IsUnique().HasDatabaseName("ix_dentist_weekly_hours_dentist_day");
        });

        modelBuilder.Entity<DentistScheduleException>(entity =>
        {
            entity.ToTable("dentist_schedule_exceptions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.DentistId).HasColumnName("dentist_id");
            entity.Property(x => x.Date).HasColumnName("date");
            entity.Property(x => x.IsClosed).HasColumnName("is_closed");
            entity.Property(x => x.StartTime).HasColumnName("start_time");
            entity.Property(x => x.EndTime).HasColumnName("end_time");
            entity.HasIndex(x => new { x.DentistId, x.Date }).IsUnique().HasDatabaseName("ix_dentist_schedule_exceptions_dentist_date");
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("appointments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.DentistId).HasColumnName("dentist_id");
            entity.Property(x => x.PatientId).HasColumnName("patient_id");
            entity.Property(x => x.ServiceId).HasColumnName("service_id");
            entity.Property(x => x.StartAtUtc).HasColumnName("start_at_utc");
            entity.Property(x => x.EndAtUtc).HasColumnName("end_at_utc");
            entity.Property(x => x.Type).HasColumnName("type").HasConversion<string>();
            entity.Property(x => x.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(x => x.PriceCents).HasColumnName("price_cents");
            entity.Property(x => x.InsuranceName).HasColumnName("insurance_name").HasMaxLength(120);
            entity.Property(x => x.InsuranceCardNumber).HasColumnName("insurance_card_number").HasMaxLength(80);
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.HasIndex(x => new { x.DentistId, x.StartAtUtc, x.EndAtUtc }).HasDatabaseName("ix_appointments_dentist_interval");
            entity.HasIndex(x => new { x.PatientId, x.StartAtUtc }).HasDatabaseName("ix_appointments_patient_start");
        });
    }
}
