// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using easpace.Desktop.Data;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Wellness.Repositories;

internal class WellnessExerciseRepository(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ILogger<WellnessExerciseRepository> logger) : IWellnessExerciseRepository
{
    public async Task<WellnessExercise> CreateWellnessExerciseAsync(UpsertWellnessExerciseRequest upsertExerciseRequest)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            logger.LogInformation("Creating new wellness exercise with '{Name}' name", upsertExerciseRequest.Name);

            var exerciseId = Guid.NewGuid();
            var wellnessExercise = new WellnessExercise
            {
                Id = exerciseId,
                CreatedAt = DateTimeOffset.Now,
                Name = upsertExerciseRequest.Name,
                Description = upsertExerciseRequest.Description,
                DefaultCycleCount = upsertExerciseRequest.DefaultCycleCount,
                Instructions = BuildInstructions(exerciseId, upsertExerciseRequest.Instructions)
            };

            dbContext.WellnessExercises.Add(wellnessExercise);
            await dbContext.SaveChangesAsync();

            return wellnessExercise;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create wellness exercise");
            throw;
        }
    }

    public async Task<IReadOnlyList<WellnessExercise>> GetWellnessExercisesAsync()
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            logger.LogInformation("Fetching all wellness exercises from database");

            var exercises = await dbContext.WellnessExercises
                .Include(e => e.Instructions.OrderBy(i => i.Order))
                .AsNoTracking()
                .ToListAsync();

            return exercises
                .OrderByDescending(e => e.CreatedAt)
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch wellness exercises from database");
            throw;
        }
    }

    public async Task<WellnessExercise?> UpdateWellnessExerciseAsync(
        Guid exerciseId,
        UpsertWellnessExerciseRequest upsertExerciseRequest)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            var exercise = await dbContext.WellnessExercises
                .Include(e => e.Instructions)
                .SingleOrDefaultAsync(e => e.Id == exerciseId);

            if (exercise == null)
            {
                logger.LogWarning("Attempted to update non-existent exercise with ID {Id}", exerciseId);
                return null;
            }

            exercise.Name = upsertExerciseRequest.Name.Trim();
            exercise.Description = upsertExerciseRequest.Description.Trim();
            exercise.DefaultCycleCount = upsertExerciseRequest.DefaultCycleCount;

            dbContext.WellnessExerciseInstructions.RemoveRange(exercise.Instructions);
            exercise.Instructions.Clear();
            var replacementInstructions = BuildInstructions(exerciseId, upsertExerciseRequest.Instructions);
            
            exercise.Instructions = replacementInstructions;
            
            dbContext.WellnessExerciseInstructions.AddRange(replacementInstructions);

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Exercise with ID {Id} successfully updated", exerciseId);
            return exercise;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update exercise with ID {Id}", exerciseId);
            throw;
        }
    }

    public async Task<bool> DeleteWellnessExerciseAsync(Guid exerciseId)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            var exercise = await dbContext.WellnessExercises.FindAsync(exerciseId);

            if (exercise is null)
            {
                logger.LogWarning("Attempted to delete non-existent exercise with ID {Id}", exerciseId);
                return false;
            }

            dbContext.WellnessExercises.Remove(exercise);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Exercise with ID {Id} successfully deleted", exerciseId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete exercise with ID {Id}", exerciseId);
            throw;
        }
    }

    private static List<WellnessExerciseInstruction> BuildInstructions(
        Guid exerciseId,
        IReadOnlyList<UpsertWellnessExerciseInstructionRequest> upsertInstructionRequests)
    {
        return upsertInstructionRequests.Select((instruction, index) => new WellnessExerciseInstruction
        {
            Id = Guid.NewGuid(),
            ExerciseId = exerciseId,
            Order = index,
            Text = instruction.Text.Trim(),
            DurationSeconds = instruction.DurationSeconds,
            BreathingPhase = instruction.BreathingPhase
        }).ToList();
    }
}