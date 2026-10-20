using Microsoft.EntityFrameworkCore;
using PaymentService.Domain;

namespace PaymentService.Infrastructure.Data;

public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");
            entity.HasKey(payment => payment.Id);

            entity.Property(payment => payment.Id)
                .HasColumnName("id");
            entity.Property(payment => payment.AppointmentId)
                .HasColumnName("appointment_id")
                .IsRequired();
            entity.Property(payment => payment.Amount)
                .HasColumnName("amount")
                .HasPrecision(18, 2)
                .IsRequired();
            entity.Property(payment => payment.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .IsRequired();
            entity.Property(payment => payment.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();
            entity.Property(payment => payment.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();
            entity.Property(payment => payment.ProcessedAt)
                .HasColumnName("processed_at");

            entity.HasIndex(payment => payment.AppointmentId)
                .HasDatabaseName("ix_payments_appointment_id");
        });
    }
}
