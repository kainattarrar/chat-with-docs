using System.Text.Json;
using ChatWithDocs.Application.Common;
using ChatWithDocs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ChatWithDocs.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Chunk> Chunks => Set<Chunk>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.FileName).IsRequired();
            entity.Property(d => d.Status).HasConversion<string>().IsRequired();
        });

        modelBuilder.Entity<Chunk>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Content).IsRequired();
            entity.Property(c => c.Embedding).HasColumnType($"vector({Constants.EmbeddingDimensions})");

            entity.HasOne(c => c.Document)
                .WithMany(d => d.Chunks)
                .HasForeignKey(c => c.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(c => c.Id);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Content).IsRequired();
            entity.Property(m => m.Role).HasConversion<string>().IsRequired();

            // Sources are denormalized (no FK to Chunk/Document) so a message keeps
            // showing what it cited even after the source document is deleted.
            entity.Property(m => m.Sources)
                .HasColumnType("jsonb")
                .HasConversion(
                    sources => JsonSerializer.Serialize(sources, (JsonSerializerOptions?)null),
                    json => JsonSerializer.Deserialize<List<MessageSource>>(json, (JsonSerializerOptions?)null) ?? new List<MessageSource>())
                .Metadata.SetValueComparer(new ValueComparer<List<MessageSource>>(
                    (a, b) => (a ?? new List<MessageSource>()).SequenceEqual(b ?? new List<MessageSource>()),
                    v => v.Aggregate(0, (hash, source) => HashCode.Combine(hash, source)),
                    v => v.ToList()));

            entity.HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
