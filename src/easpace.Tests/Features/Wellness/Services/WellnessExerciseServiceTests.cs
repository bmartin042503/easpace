// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Data;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Features.Wellness.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace easpace.Tests.Features.Wellness.Services;

public class WellnessExerciseServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:;Foreign Keys=True");
    private readonly InMemoryDbContextFactory _dbContextFactory;
    private readonly WellnessExerciseService _service;

    public WellnessExerciseServiceTests()
    {
        _dbContextFactory = new InMemoryDbContextFactory(_connection);
        _service = new WellnessExerciseService(_dbContextFactory, new Mock<ILogger<WellnessExerciseService>>().Object);
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

    private static UpsertWellnessExerciseRequest BreathingRequest(string name = "Calm Breath") =>
        new(null, name, "Slow down", true,
        [
            new UpsertExerciseInstructionRequest("", 4, BreathingPhaseType.Inhale),
            new UpsertExerciseInstructionRequest("Hold gently", 2, BreathingPhaseType.HoldIn),
            new UpsertExerciseInstructionRequest("", 6, BreathingPhaseType.Exhale)
        ]);

    private static UpsertWellnessExerciseRequest MeditationRequest(string name = "Quiet Mind") =>
        new(null, name, "Rest", false,
        [
            new UpsertExerciseInstructionRequest("Notice your breath", 60, null),
            new UpsertExerciseInstructionRequest("Relax your shoulders", 90, null)
        ]);

    private static UpsertWellnessExerciseRequest ToRequest(WellnessExercise exercise) =>
        new(null, exercise.Name, exercise.Description, exercise.IsRepeating,
            exercise.Instructions
                .OrderBy(i => i.Order)
                .Select(i => new UpsertExerciseInstructionRequest(i.Text, i.DurationSeconds, i.Phase))
                .ToList());

    private async Task<int> CountAsync<T>() where T : class
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        return await dbContext.Set<T>().CountAsync(TestCancellation);
    }

    [Fact]
    public async Task CreateExerciseAsync_PersistsTrimmedValuesAndOrderedInstructions()
    {
        var request = BreathingRequest() with { Name = "  Calm Breath  ", Description = "  Slow down  " };

        var created = await _service.CreateExerciseAsync(request, TestCancellation);

        var exercise = (await _service.GetExercisesAsync(TestCancellation)).Single();
        exercise.Id.Should().Be(created.Id);
        exercise.Name.Should().Be("Calm Breath");
        exercise.Description.Should().Be("Slow down");
        exercise.IsRepeating.Should().BeTrue();
        exercise.Instructions
            .Select(i => (i.Order, i.Text, i.DurationSeconds, i.Phase))
            .Should().Equal(
                (1, "", 4, BreathingPhaseType.Inhale),
                (2, "Hold gently", 2, BreathingPhaseType.HoldIn),
                (3, "", 6, BreathingPhaseType.Exhale));
    }

    [Fact]
    public async Task CreateExerciseAsync_KeepsTheBreathingPhasesOfTextInstructions()
    {
        var request = MeditationRequest() with
        {
            Instructions =
            [
                new UpsertExerciseInstructionRequest("Breathe in slowly", 30, BreathingPhaseType.Inhale),
                new UpsertExerciseInstructionRequest("Rest", 60, null)
            ]
        };

        await _service.CreateExerciseAsync(request, TestCancellation);

        var exercise = (await _service.GetExercisesAsync(TestCancellation)).Single();
        exercise.Instructions.OrderBy(i => i.Order).Select(i => (i.Text, i.Phase))
            .Should().Equal(("Breathe in slowly", BreathingPhaseType.Inhale), ("Rest", (BreathingPhaseType?)null));
    }

    [Fact]
    public async Task CreateExerciseAsync_WithRequestId_UsesIt()
    {
        var id = Guid.NewGuid();

        var created = await _service.CreateExerciseAsync(BreathingRequest() with { Id = id }, TestCancellation);

        created.Id.Should().Be(id);
        created.Instructions.Should().OnlyContain(i => i.ExerciseId == id);
    }

    [Theory]
    [InlineData("blank name")]
    [InlineData("name too long")]
    [InlineData("description too long")]
    [InlineData("no instructions")]
    [InlineData("zero duration")]
    [InlineData("duration too long")]
    [InlineData("text too long")]
    [InlineData("no text and no phase")]
    [InlineData("cycle too long")]
    public async Task CreateExerciseAsync_WithInvalidRequest_ThrowsAndPersistsNothing(string invalidCase)
    {
        var breathing = BreathingRequest();
        var request = invalidCase switch
        {
            "blank name" => breathing with { Name = "   " },
            "name too long" => breathing with { Name = new string('a', 65) },
            "description too long" => breathing with { Description = new string('a', 257) },
            "no instructions" => breathing with { Instructions = [] },
            "zero duration" => breathing with { Instructions = [new("Breathe", 0, BreathingPhaseType.Inhale)] },
            "duration too long" => breathing with { Instructions = [new("Breathe", 3601, BreathingPhaseType.Inhale)] },
            "text too long" => breathing with { Instructions = [new(new string('a', 257), 4, BreathingPhaseType.Inhale)] },
            "no text and no phase" => breathing with { Instructions = [new("  ", 4, null)] },
            "cycle too long" => breathing with { Instructions = [new("One", 1800, null), new("Two", 1801, null)] },
            _ => throw new ArgumentOutOfRangeException(nameof(invalidCase))
        };

        var act = () => _service.CreateExerciseAsync(request, TestCancellation);

        await act.Should().ThrowAsync<ArgumentException>();
        (await CountAsync<WellnessExercise>()).Should().Be(0);
    }

    [Fact]
    public async Task CreateExerciseAsync_WithBoundaryValues_Succeeds()
    {
        var request = BreathingRequest() with
        {
            Name = new string('a', 64),
            Description = new string('b', 256),
            Instructions = [new(new string('c', 256), 1800, null), new("", 1800, BreathingPhaseType.Exhale)]
        };

        var act = () => _service.CreateExerciseAsync(request, TestCancellation);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetExercisesAsync_ReturnsExercisesSortedByNameIgnoringCase()
    {
        await _service.CreateExerciseAsync(BreathingRequest("gamma"), TestCancellation);
        await _service.CreateExerciseAsync(MeditationRequest("Alpha"), TestCancellation);
        await _service.CreateExerciseAsync(BreathingRequest("beta"), TestCancellation);

        var exercises = await _service.GetExercisesAsync(TestCancellation);

        exercises.Select(e => e.Name).Should().Equal("Alpha", "beta", "gamma");
    }

    [Fact]
    public async Task UpdateExerciseAsync_ReplacesPropertiesAndInstructions()
    {
        var created = await _service.CreateExerciseAsync(BreathingRequest(), TestCancellation);
        var request = new UpsertWellnessExerciseRequest(created.Id, "  Renamed  ", "  New description  ", false,
            [
                new UpsertExerciseInstructionRequest("Exhale fully", 5, BreathingPhaseType.Exhale),
                new UpsertExerciseInstructionRequest("", 3, BreathingPhaseType.HoldOut)
            ]);

        await _service.UpdateExerciseAsync(created.Id, request, TestCancellation);

        var exercise = (await _service.GetExercisesAsync(TestCancellation)).Single();
        exercise.Name.Should().Be("Renamed");
        exercise.Description.Should().Be("New description");
        exercise.IsRepeating.Should().BeFalse();
        exercise.Instructions
            .Select(i => (i.Order, i.Text, i.DurationSeconds, i.Phase))
            .Should().Equal(
                (1, "Exhale fully", 5, BreathingPhaseType.Exhale),
                (2, "", 3, BreathingPhaseType.HoldOut));

        // the previous instructions are removed rather than kept alongside the new ones
        (await CountAsync<ExerciseInstruction>()).Should().Be(2);
    }

    [Fact]
    public async Task UpdateExerciseAsync_WithUnknownId_Throws()
    {
        var act = () => _service.UpdateExerciseAsync(Guid.NewGuid(), BreathingRequest(), TestCancellation);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateExerciseAsync_WithInvalidRequest_KeepsStoredExercise()
    {
        var created = await _service.CreateExerciseAsync(BreathingRequest(), TestCancellation);

        var act = () => _service.UpdateExerciseAsync(created.Id, BreathingRequest() with { Instructions = [] }, TestCancellation);

        await act.Should().ThrowAsync<ArgumentException>();
        (await CountAsync<ExerciseInstruction>()).Should().Be(3);
    }

    [Fact]
    public async Task DeleteExerciseAsync_RemovesInstructionsAndDetachesSessionsKeepingTheirName()
    {
        var deleted = await _service.CreateExerciseAsync(BreathingRequest("Calm Breath"), TestCancellation);
        var kept = await _service.CreateExerciseAsync(MeditationRequest(), TestCancellation);
        var sessionId = Guid.NewGuid();

        await using (var dbContext = _dbContextFactory.CreateDbContext())
        {
            dbContext.WellnessSessionEntries.Add(new WellnessSessionEntry
            {
                Id = sessionId,
                StartDate = DateTimeOffset.Now,
                ActualDuration = TimeSpan.FromMinutes(2),
                ExerciseId = deleted.Id,
                ExerciseName = "Calm Breath"
            });
            await dbContext.SaveChangesAsync(TestCancellation);
        }

        await _service.DeleteExerciseAsync(deleted.Id, TestCancellation);

        (await _service.GetExercisesAsync(TestCancellation)).Select(e => e.Id).Should().Equal(kept.Id);
        (await CountAsync<ExerciseInstruction>()).Should().Be(kept.Instructions.Count);

        await using (var dbContext = _dbContextFactory.CreateDbContext())
        {
            var session = await dbContext.WellnessSessionEntries.SingleAsync(s => s.Id == sessionId, TestCancellation);
            session.ExerciseId.Should().BeNull();
            session.ExerciseName.Should().Be("Calm Breath");
        }
    }

    [Fact]
    public async Task DeleteExerciseAsync_WithUnknownId_DoesNothing()
    {
        await _service.CreateExerciseAsync(BreathingRequest(), TestCancellation);

        await _service.DeleteExerciseAsync(Guid.NewGuid(), TestCancellation);

        (await CountAsync<WellnessExercise>()).Should().Be(1);
    }

    [Fact]
    public void DefaultWellnessExercises_Create_ProducesValidBuiltInExercises()
    {
        var defaults = DefaultWellnessExercises.Create();

        defaults.Should().HaveCount(6);
        defaults.Select(e => e.Name).Should().OnlyHaveUniqueItems();

        // four breathing rhythms made of phases, two meditations made of texts
        defaults.Count(e => e.Instructions.All(i => i.Phase != null)).Should().Be(4);
        defaults.Count(e => e.Instructions.All(i => i.Phase == null)).Should().Be(2);

        foreach (var exercise in defaults)
        {
            exercise.Name.Should().NotBeNullOrWhiteSpace().And.NotStartWith("[");
            exercise.Name.Length.Should().BeLessThanOrEqualTo(64);
            exercise.Description.Should().NotBeNullOrWhiteSpace().And.NotStartWith("[");
            exercise.Description.Length.Should().BeLessThanOrEqualTo(256);
            exercise.IsRepeating.Should().BeTrue();

            exercise.Instructions.Should().NotBeEmpty();
            exercise.Instructions.Select(i => i.Order).Should().Equal(Enumerable.Range(1, exercise.Instructions.Count));
            exercise.Instructions.Sum(i => i.DurationSeconds).Should().BeLessThanOrEqualTo(3600);

            foreach (var instruction in exercise.Instructions)
            {
                instruction.Text.Should().NotBeNullOrWhiteSpace().And.NotStartWith("[");
                instruction.Text.Length.Should().BeLessThanOrEqualTo(256);
                instruction.DurationSeconds.Should().BeInRange(1, 3600);
            }
        }
    }

    [Fact]
    public void DefaultWellnessExercises_Create_ReturnsFreshInstances()
    {
        var first = DefaultWellnessExercises.Create();
        var second = DefaultWellnessExercises.Create();

        first.Select(e => e.Id).Should().NotIntersectWith(second.Select(e => e.Id));
        first.SelectMany(e => e.Instructions).Select(i => i.Id)
            .Should().NotIntersectWith(second.SelectMany(e => e.Instructions).Select(i => i.Id));
    }

    [Fact]
    public async Task DefaultWellnessExercises_PassServiceValidation()
    {
        foreach (var exercise in DefaultWellnessExercises.Create())
        {
            var act = () => _service.CreateExerciseAsync(ToRequest(exercise), TestCancellation);

            await act.Should().NotThrowAsync();
        }
    }

    private sealed class InMemoryDbContextFactory(SqliteConnection connection) : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options =
            new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        public AppDbContext CreateDbContext() => new(_options);
    }
}
