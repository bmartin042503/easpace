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
    public DbSet<WellnessSessionEntry> WellnessSessionEntries { get; set; }
    public DbSet<WellnessExercise> WellnessExercises { get; set; }
    public DbSet<ExerciseInstruction> ExerciseInstructions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Activity>(entity => 
        {
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
            .UseTphMappingStrategy()
            .HasDiscriminator<string>("EntryType")
            .HasValue<NumericActivityDataEntry>("Numeric")
            .HasValue<RoutineActivityDataEntry>("Routine");
        
        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.Property(e => e.Title)
                .HasMaxLength(128);
            
            entity.Property(e => e.Content)
                .HasMaxLength(12800); 
        });
        
        modelBuilder.Entity<MoodEntry>(entity =>
        {
            entity.Property(m => m.Description)
                .HasMaxLength(512);
        });
        
        modelBuilder.Entity<WellnessExercise>(entity =>
        {
            entity.HasMany(e => e.Instructions)
                .WithOne()
                .HasForeignKey(i => i.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Name)
                .HasMaxLength(64);

            entity.Property(e => e.Description)
                .HasMaxLength(256);
        });

        modelBuilder.Entity<ExerciseInstruction>()
            .Property(i => i.Text)
            .HasMaxLength(256);

        modelBuilder.Entity<WellnessSessionEntry>(entity =>
        {
            entity.HasOne(s => s.Exercise)
                .WithMany()
                .HasForeignKey(s => s.ExerciseId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(s => s.ExerciseName)
                .HasMaxLength(64);
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
#if DEBUG
        optionsBuilder.EnableSensitiveDataLogging();
#endif
    }
}