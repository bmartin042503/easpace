// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Entities;
using easpace.Desktop.Features.Activities.Entities.DataEntries;
using easpace.Desktop.Features.Journal.Entities;
using easpace.Desktop.Features.Mood.Entities;
using easpace.Desktop.Features.Wellness.Entities;
using Microsoft.EntityFrameworkCore;

namespace easpace.Desktop.Data;

internal class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Activities
    public DbSet<Activity> Activities { get; set; }
    public DbSet<ActivityDataEntry> ActivityDataEntries { get; set; }

    // Journal
    public DbSet<JournalEntry> JournalEntries { get; set; }

    // Mood
    public DbSet<MoodEntry> MoodEntries { get; set; }

    // Wellness
    public DbSet<WellnessExercise> WellnessExercises { get; set; }
    public DbSet<WellnessExerciseInstruction> WellnessExerciseInstructions { get; set; }
    public DbSet<WellnessSessionEntry> WellnessSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Activity
        modelBuilder.Entity<Activity>(entity =>
        {
            entity.ToTable("Activities"); 
            
            entity.HasMany(a => a.Entries)
                .WithOne()
                .HasForeignKey(a => a.ActivityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(a => a.Name)
                .HasMaxLength(64);
        });

        modelBuilder.Entity<Activity>()
            .UseTphMappingStrategy()
            .HasDiscriminator<string>("ActivityType")
            .HasValue<TrendActivity>("Trend")
            .HasValue<MilestoneActivity>("Milestone")
            .HasValue<RoutineActivity>("Routine");

        modelBuilder.Entity<NumericActivity>()
            .Property(a => a.Unit)
            .HasMaxLength(16);

        modelBuilder.Entity<TrendActivity>(entity =>
        {
            entity.Property(a => a.Aggregation)
                .HasDefaultValue(TrendAggregation.Average)
                .HasSentinel(TrendAggregation.Average);
        });

        modelBuilder.Entity<ActivityDataEntry>()
            .ToTable("ActivityDataEntries")
            .UseTphMappingStrategy()
            .HasDiscriminator<string>("EntryType")
            .HasValue<NumericActivityDataEntry>("Numeric")
            .HasValue<RoutineActivityDataEntry>("Routine");

        // Journal
        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.ToTable("JournalEntries");
            
            entity.Property(e => e.Title)
                .HasMaxLength(128);

            entity.Property(e => e.Content)
                .HasMaxLength(12800);
        });

        // Mood
        modelBuilder.Entity<MoodEntry>(entity =>
        {
            entity.ToTable("MoodEntries");
            
            entity.Property(m => m.Description)
                .HasMaxLength(512);
        });

        // Wellness
        modelBuilder.Entity<WellnessExercise>(entity =>
        {
            entity.ToTable("WellnessExercises");

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(512);

            entity.HasMany(e => e.Instructions)
                .WithOne()
                .HasForeignKey(i => i.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WellnessExerciseInstruction>(entity =>
        {
            entity.ToTable("WellnessExerciseInstructions");

            entity.Property(i => i.Text)
                .HasMaxLength(256);

            entity.HasIndex(i => new { i.ExerciseId, i.Order });
        });

        modelBuilder.Entity<WellnessSessionEntry>(entity =>
        {
            entity.ToTable("WellnessSessionEntries");

            entity.Property(s => s.ExerciseName)
                .IsRequired()
                .HasMaxLength(64);

            entity.HasOne(s => s.Exercise)
                .WithMany()
                .HasForeignKey(s => s.ExerciseId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
#if DEBUG
        optionsBuilder.EnableSensitiveDataLogging();
#endif
    }
}