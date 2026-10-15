using Microsoft.EntityFrameworkCore;

namespace Messaging.Outbox;

public static class OutboxModelBuilderExtensions
{
    public static ModelBuilder ApplyOutboxConfiguration(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Topic).HasColumnName("topic").HasMaxLength(249).IsRequired();
            entity.Property(x => x.Key).HasColumnName("key").HasMaxLength(200).IsRequired();
            entity.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(200).IsRequired();
            entity.Property(x => x.Payload).HasColumnName("payload").IsRequired();
            entity.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc");
            entity.Property(x => x.ProcessedAtUtc).HasColumnName("processed_at_utc");
            entity.Property(x => x.Attempts).HasColumnName("attempts");
            entity.Property(x => x.LastError).HasColumnName("last_error").HasMaxLength(OutboxMessage.LastErrorMaxLength);
            entity.HasIndex(x => new { x.ProcessedAtUtc, x.OccurredAtUtc }).HasDatabaseName("ix_outbox_messages_pending");
        });

        return modelBuilder;
    }
}
