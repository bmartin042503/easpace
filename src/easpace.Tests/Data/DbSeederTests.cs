// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Constants;
using easpace.Desktop.Data;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Features.Wellness.Services;
using easpace.Desktop.Services.Data;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace easpace.Tests.Data;

public class DbSeederTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:;Foreign Keys=True");
    private readonly InMemoryDbContextFactory _dbContextFactory;
    private readonly Mock<IPreferencesService> _preferencesServiceMock = new();
    private readonly DbSeeder _seeder;

    public DbSeederTests()
    {
        _dbContextFactory = new InMemoryDbContextFactory(_connection);
        _seeder = new DbSeeder(_dbContextFactory, _preferencesServiceMock.Object);
    }

    private static CancellationToken TestCancellation => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await _connection.OpenAsync(TestCancellation);

        await using var dbContext = _dbContextFactory.CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(TestCancellation);
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private void SetSeededFlag(bool isSeeded) =>
        _preferencesServiceMock
            .Setup(p => p.ReadPreference(PreferenceKey.WellnessDefaultsSeeded, It.IsAny<bool>()))
            .Returns(isSeeded);

    private async Task<BreathingExercise> AddBreathingExerciseAsync()
    {
        var exercise = new BreathingExercise
        {
            Name = "Migrated Breathing",
            IsRepeating = true,
            Instructions = [new ExerciseInstruction { Order = 1, DurationSeconds = 4, Phase = BreathingPhaseType.Inhale }]
        };

        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.WellnessExercises.Add(exercise);
        await dbContext.SaveChangesAsync(TestCancellation);

        return exercise;
    }

    private async Task<List<WellnessExercise>> GetExercisesAsync()
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        return await dbContext.WellnessExercises
            .Include(e => e.Instructions)
            .AsNoTracking()
            .ToListAsync(TestCancellation);
    }

    [Fact]
    public async Task SeedAsync_OnFreshDatabase_SeedsDefaultsOfBothTypesAndSetsFlag()
    {
        SetSeededFlag(false);

        await _seeder.SeedAsync();

        var exercises = await GetExercisesAsync();
        var defaults = DefaultWellnessExercises.Create();
        exercises.Should().Contain(e => e is BreathingExercise).And.Contain(e => e is MeditationExercise);
        exercises.Select(e => (e.SessionType, e.Name, e.Instructions.Count))
            .Should().BeEquivalentTo(defaults.Select(e => (e.SessionType, e.Name, e.Instructions.Count)));
        _preferencesServiceMock.Verify(p => p.SavePreference(PreferenceKey.WellnessDefaultsSeeded, true), Times.Once);
    }

    [Fact]
    public async Task SeedAsync_WithFlagSet_LeavesExistingDatabaseUntouched()
    {
        SetSeededFlag(true);
        var existing = await AddBreathingExerciseAsync();

        await _seeder.SeedAsync();

        (await GetExercisesAsync()).Should().ContainSingle().Which.Id.Should().Be(existing.Id);
        _preferencesServiceMock.Verify(p => p.SavePreference(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task SeedAsync_OnUpgradedDatabaseWithBreathingOnly_SeedsOnlyMeditationDefaultsAndSetsFlag()
    {
        SetSeededFlag(false);
        var migrated = await AddBreathingExerciseAsync();

        await _seeder.SeedAsync();

        var exercises = await GetExercisesAsync();
        exercises.OfType<BreathingExercise>().Should().ContainSingle().Which.Id.Should().Be(migrated.Id);
        exercises.OfType<MeditationExercise>().Select(e => e.Name)
            .Should().BeEquivalentTo(DefaultWellnessExercises.Create().OfType<MeditationExercise>().Select(e => e.Name));
        _preferencesServiceMock.Verify(p => p.SavePreference(PreferenceKey.WellnessDefaultsSeeded, true), Times.Once);
    }

    [Fact]
    public async Task SeedAsync_WithFlagSetOnEmptyDatabase_KeepsDeletedDefaultsDeleted()
    {
        SetSeededFlag(true);

        await _seeder.SeedAsync();

        (await GetExercisesAsync()).Should().BeEmpty();
        _preferencesServiceMock.Verify(p => p.SavePreference(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    private sealed class InMemoryDbContextFactory(SqliteConnection connection) : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options =
            new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        public AppDbContext CreateDbContext() => new(_options);
    }
}
