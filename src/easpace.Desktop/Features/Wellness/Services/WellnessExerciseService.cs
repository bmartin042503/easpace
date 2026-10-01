// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using easpace.Desktop.Data;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Wellness.Services;

internal class WellnessExerciseService : IWellnessExerciseService
{
    // mirror the model configuration, because SQLite doesn't enforce maximum lengths
    private const int NameMaxLength = 64;
    private const int DescriptionMaxLength = 256;
    private const int InstructionTextMaxLength = 256;

    // applies to a single instruction and to a whole cycle
    private const int MaxDurationSeconds = 60 * 60;

    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger<WellnessExerciseService> _logger;

    public WellnessExerciseService(
        IDbContextFactory<AppDbContext> dbContextFactory,
        ILogger<WellnessExerciseService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<WellnessExercise>> GetExercisesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            _logger.LogInformation("Fetching wellness exercises");

            var exercises = await dbContext.WellnessExercises
                .Include(e => e.Instructions.OrderBy(i => i.Order))
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return exercises.OrderBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch wellness exercises from database");
            throw;
        }
    }

    public async Task<WellnessExercise> CreateExerciseAsync(
        UpsertWellnessExerciseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var exercise = new WellnessExercise();

            exercise.Id = request.Id ?? exercise.Id;
            exercise.CreatedAt = DateTimeOffset.Now;
            ApplyRequest(exercise, request);

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            _logger.LogInformation("Creating new wellness exercise '{Name}'", exercise.Name);

            dbContext.WellnessExercises.Add(exercise);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Wellness exercise created successfully with ID {Id}", exercise.Id);
            return exercise;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create wellness exercise");
            throw;
        }
    }

    public async Task<WellnessExercise> UpdateExerciseAsync(
        Guid id,
        UpsertWellnessExerciseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            var exercise = await dbContext.WellnessExercises
                .Include(e => e.Instructions)
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
                ?? throw new KeyNotFoundException($"Wellness exercise with ID {id} does not exist.");

            // the whole instruction list is replaced, as reordering and editing are saved together
            dbContext.ExerciseInstructions.RemoveRange(exercise.Instructions);
            ApplyRequest(exercise, request);

            // new instructions already have their keys, so EF would treat them as existing rows unless added explicitly
            dbContext.ExerciseInstructions.AddRange(exercise.Instructions);

            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Wellness exercise with ID {Id} successfully updated", id);
            return exercise;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update wellness exercise with ID {Id}", id);
            throw;
        }
    }

    public async Task DeleteExerciseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            var exercise = await dbContext.WellnessExercises.FindAsync([id], cancellationToken);

            if (exercise is null)
            {
                _logger.LogWarning("Attempted to delete non-existent wellness exercise with ID {Id}", id);
                return;
            }

            // instructions are deleted by cascade, sessions keep their ExerciseName snapshot (ExerciseId is set to null)
            dbContext.WellnessExercises.Remove(exercise);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Wellness exercise with ID {Id} successfully deleted", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete wellness exercise with ID {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Copies the normalized request values onto the exercise, replacing its instructions, and validates the result.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the resulting exercise violates the exercise constraints.</exception>
    private static void ApplyRequest(WellnessExercise exercise, UpsertWellnessExerciseRequest request)
    {
        exercise.Name = request.Name.Trim();
        exercise.Description = request.Description.Trim();
        exercise.IsRepeating = request.IsRepeating;
        exercise.Instructions = request.Instructions
            .Select((instruction, index) => new ExerciseInstruction
            {
                ExerciseId = exercise.Id,
                Order = index + 1,
                Text = instruction.Text.Trim(),
                DurationSeconds = instruction.DurationSeconds,
                Phase = instruction.Phase
            })
            .ToList();

        if (GetValidationError(exercise) is { } validationError)
        {
            throw new ArgumentException(validationError, nameof(request));
        }
    }

    private static string? GetValidationError(WellnessExercise exercise)
    {
        if (exercise.Name.Length is 0 or > NameMaxLength)
        {
            return $"The exercise name must be between 1 and {NameMaxLength} characters long.";
        }

        if (exercise.Description.Length > DescriptionMaxLength)
        {
            return $"The exercise description must not exceed {DescriptionMaxLength} characters.";
        }

        if (exercise.Instructions.Count == 0)
        {
            return "An exercise requires at least one instruction.";
        }

        foreach (var instruction in exercise.Instructions)
        {
            if (instruction.DurationSeconds is < 1 or > MaxDurationSeconds)
            {
                return $"Instruction durations must be between 1 and {MaxDurationSeconds} seconds.";
            }

            if (instruction.Text.Length > InstructionTextMaxLength)
            {
                return $"Instruction texts must not exceed {InstructionTextMaxLength} characters.";
            }

            if (instruction.Text.Length == 0 && instruction.Phase is null)
            {
                return "Instructions without a breathing phase require a text.";
            }
        }

        return exercise.Instructions.Sum(i => i.DurationSeconds) > MaxDurationSeconds
            ? $"A single cycle must not exceed {MaxDurationSeconds} seconds."
            : null;
    }
}
