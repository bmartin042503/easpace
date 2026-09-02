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
    public DbSet<BreathingPhase> BreathingPhases { get; set; }
    public DbSet<BreathingTechnique> BreathingTechniques { get; set; }
    public DbSet<WellnessSessionEntry> WellnessSessionEntries { get; set; }

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
        
        modelBuilder.Entity<BreathingTechnique>(entity =>
        {

            entity.HasMany(t => t.Phases)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.Property(t => t.Name)
                .HasMaxLength(64);
            
            entity.Property(t => t.Description)
                .HasMaxLength(256);
        });
        
        modelBuilder.Entity<WellnessSessionEntry>()
            .HasOne(s => s.BreathingTechnique)
            .WithMany()
            .HasForeignKey(s => s.BreathingTechniqueId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
#if DEBUG
        optionsBuilder.EnableSensitiveDataLogging();
#endif
    }
}