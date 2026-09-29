// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.ViewModels;
using easpace.Desktop.Services.Core;
using FluentAssertions;

namespace easpace.Tests.Features.Wellness;

public class ExercisePreviewViewModelTests
{
    private static readonly IReadOnlyList<ExerciseStep> BoxSteps =
    [
        ExerciseStep.Create(null, 4, BreathingPhaseType.Inhale),
        ExerciseStep.Create(null, 4, BreathingPhaseType.HoldIn),
        ExerciseStep.Create(null, 4, BreathingPhaseType.Exhale),
        ExerciseStep.Create(null, 4, BreathingPhaseType.HoldOut)
    ];

    private static readonly IReadOnlyList<ExerciseStep> MeditationSteps =
    [
        ExerciseStep.Create("Sit", 60, null),
        ExerciseStep.Create("Breathe", 30, null)
    ];

    private static ExercisePreviewViewModel CreateLoadedPreview(IReadOnlyList<ExerciseStep> steps, WellnessSessionType type)
    {
        var preview = new ExercisePreviewViewModel();
        preview.Load(steps, type);
        return preview;
    }

    // runs the dispatcher loop, timers included, long enough for the one-second session timer to tick if it's running
    private static void RunDispatcherFor(TimeSpan duration)
    {
        var frame = new DispatcherFrame();
        var stopTimer = new DispatcherTimer(duration, DispatcherPriority.Background, (_, _) => frame.Continue = false);

        stopTimer.Start();
        Dispatcher.UIThread.PushFrame(frame);
        stopTimer.Stop();
    }

    [AvaloniaFact]
    public void Load_ShowsTheFirstStepPaused()
    {
        var preview = CreateLoadedPreview(BoxSteps, WellnessSessionType.Breathing);

        preview.HasSteps.Should().BeTrue();
        preview.IsPlaying.Should().BeFalse();
        preview.Progress!.StepIndex.Should().Be(0);
        preview.Progress.Cycles.Should().BeNull();
        preview.InstructionText.Should().Be(ExerciseStep.GetDefaultText(BreathingPhaseType.Inhale));
        preview.StepSecondsText.Should().Be("4");
        preview.PositionText.Should().Be(string.Format(LocalizationService.GetString("Wellness.Preview.Position"), 1, 4, 1));
        preview.ShowBreathingCircle.Should().BeTrue();
        preview.ShowCountdown.Should().BeFalse();
        preview.CircleSize.Should().Be(96);
        preview.TogglePlayPauseCommand.CanExecute(null).Should().BeTrue();
        preview.StepBackwardCommand.CanExecute(null).Should().BeFalse();
    }

    [AvaloniaFact]
    public void Load_WithMeditation_ShowsTheCountdownInsteadOfTheCircle()
    {
        var preview = CreateLoadedPreview(MeditationSteps, WellnessSessionType.Meditation);

        preview.InstructionText.Should().Be("Sit");
        preview.StepSecondsText.Should().Be("60");
        preview.ShowBreathingCircle.Should().BeFalse();
        preview.ShowCountdown.Should().BeTrue();
    }

    [AvaloniaFact]
    public void Load_WithoutSteps_DisablesThePreview()
    {
        var preview = CreateLoadedPreview([], WellnessSessionType.Breathing);

        preview.HasSteps.Should().BeFalse();
        preview.Progress.Should().BeNull();
        preview.InstructionText.Should().BeEmpty();
        preview.ShowCountdown.Should().BeFalse();
        preview.TogglePlayPauseCommand.CanExecute(null).Should().BeFalse();
        preview.StepForwardCommand.CanExecute(null).Should().BeFalse();
        preview.RestartCommand.CanExecute(null).Should().BeFalse();
    }

    [AvaloniaFact]
    public void Steps_UpdateTheProgressAndSnapTheCircle()
    {
        var preview = CreateLoadedPreview(BoxSteps, WellnessSessionType.Breathing);

        preview.StepForwardCommand.Execute(null);
        preview.StepForwardCommand.Execute(null);

        preview.Progress!.StepIndex.Should().Be(2);
        preview.InstructionText.Should().Be(ExerciseStep.GetDefaultText(BreathingPhaseType.Exhale));
        preview.CircleSize.Should().Be(192);
        preview.StepBackwardCommand.CanExecute(null).Should().BeTrue();

        preview.StepBackwardCommand.Execute(null);

        preview.Progress.StepIndex.Should().Be(1);

        preview.RestartCommand.Execute(null);

        preview.Progress.StepIndex.Should().Be(0);
        preview.CircleSize.Should().Be(96);
        preview.StepBackwardCommand.CanExecute(null).Should().BeFalse();
    }

    [AvaloniaFact]
    public void StepForward_LoopsIntoTheNextCycle()
    {
        var preview = CreateLoadedPreview(MeditationSteps, WellnessSessionType.Meditation);

        preview.StepForwardCommand.Execute(null);
        preview.StepForwardCommand.Execute(null);

        preview.Progress!.StepIndex.Should().Be(0);
        preview.Progress.CycleIndex.Should().Be(1);
        preview.PositionText.Should().Be(string.Format(LocalizationService.GetString("Wellness.Preview.Position"), 1, 2, 2));
    }

    [AvaloniaFact]
    public void Load_KeepsThePositionAndClampsItToTheRemainingSteps()
    {
        var preview = CreateLoadedPreview(BoxSteps, WellnessSessionType.Breathing);
        for (var i = 0; i < 7; i++) preview.StepForwardCommand.Execute(null);
        preview.Progress!.CycleIndex.Should().Be(1);
        preview.Progress.StepIndex.Should().Be(3);

        preview.Load(BoxSteps.Take(2).ToList(), WellnessSessionType.Breathing);

        preview.Progress!.CycleIndex.Should().Be(1);
        preview.Progress.StepIndex.Should().Be(1);
        preview.Progress.StepCount.Should().Be(2);
        preview.InstructionText.Should().Be(ExerciseStep.GetDefaultText(BreathingPhaseType.HoldIn));
        preview.CircleSize.Should().Be(192);
    }

    [AvaloniaFact]
    public void Load_WithChangedTextsOnly_KeepsTheTimeOfTheCurrentStep()
    {
        var preview = CreateLoadedPreview(MeditationSteps, WellnessSessionType.Meditation);
        preview.TogglePlayPauseCommand.Execute(null);
        RunDispatcherFor(TimeSpan.FromSeconds(1.3));
        preview.TogglePlayPauseCommand.Execute(null);
        var secondsLeft = preview.StepSecondsText;
        secondsLeft.Should().NotBe("60");

        preview.Load([ExerciseStep.Create("Sit still", 60, null), MeditationSteps[1]], WellnessSessionType.Meditation);

        preview.InstructionText.Should().Be("Sit still");
        preview.StepSecondsText.Should().Be(secondsLeft);

        preview.Dispose();
    }

    [AvaloniaFact]
    public void Play_AdvancesTheStepsAndPauseStopsThem()
    {
        var preview = CreateLoadedPreview([ExerciseStep.Create("One", 1, null), ExerciseStep.Create("Two", 5, null)],
            WellnessSessionType.Meditation);

        preview.TogglePlayPauseCommand.Execute(null);
        preview.IsPlaying.Should().BeTrue();
        preview.TogglePlayPauseText.Should().Be(LocalizationService.GetString("Wellness.Preview.Pause"));
        RunDispatcherFor(TimeSpan.FromSeconds(1.3));

        preview.InstructionText.Should().Be("Two");

        preview.Pause();
        var paused = preview.Progress;
        RunDispatcherFor(TimeSpan.FromSeconds(1.3));

        preview.IsPlaying.Should().BeFalse();
        preview.TogglePlayPauseText.Should().Be(LocalizationService.GetString("Wellness.Preview.Play"));
        preview.Progress.Should().Be(paused);

        preview.Dispose();
    }

    [AvaloniaFact]
    public void Load_WhilePlaying_KeepsPlaying()
    {
        var preview = CreateLoadedPreview(MeditationSteps, WellnessSessionType.Meditation);
        preview.TogglePlayPauseCommand.Execute(null);

        preview.Load([ExerciseStep.Create("Sit", 10, null)], WellnessSessionType.Meditation);
        RunDispatcherFor(TimeSpan.FromSeconds(1.3));

        preview.IsPlaying.Should().BeTrue();
        preview.StepSecondsText.Should().NotBe("10");

        preview.Dispose();
    }

    [AvaloniaFact]
    public void Dispose_StopsTheTimersForGood()
    {
        var preview = CreateLoadedPreview(MeditationSteps, WellnessSessionType.Meditation);
        preview.TogglePlayPauseCommand.Execute(null);
        var changes = 0;
        preview.PropertyChanged += (_, _) => changes++;

        preview.Dispose();
        changes = 0;
        RunDispatcherFor(TimeSpan.FromSeconds(1.3));
        preview.Load(MeditationSteps, WellnessSessionType.Meditation);

        changes.Should().Be(0);
        preview.IsPlaying.Should().BeFalse();
        preview.HasSteps.Should().BeFalse();
    }
}
