// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Services;
using FluentAssertions;

namespace easpace.Tests.Features.Wellness;

public class WellnessSessionManagerTests
{
    private const double MinSize = 96;
    private const double MaxSize = 192;

    private static readonly IReadOnlyList<ExerciseStep> BoxSteps =
    [
        ExerciseStep.Create(null, 2, BreathingPhaseType.Inhale),
        ExerciseStep.Create(null, 1, BreathingPhaseType.HoldIn),
        ExerciseStep.Create(null, 2, BreathingPhaseType.Exhale),
        ExerciseStep.Create(null, 1, BreathingPhaseType.HoldOut)
    ];

    private static WellnessSessionManager CreateManager(int? cycles) =>
        new(new WellnessSessionConfiguration(WellnessSessionType.Breathing, null, "Box", BoxSteps, cycles, cycles is not null));

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
    public void StepForward_JumpsToTheStartOfTheNextStepAndReportsIt()
    {
        var manager = CreateManager(3);
        var reported = new List<SessionProgress>();
        var circleSizes = new List<double>();
        manager.ProgressChanged += (_, progress) => reported.Add(progress);
        manager.BreathingCircleAnimationTimerTick += (_, size) => circleSizes.Add(size);

        manager.StepForward().Should().BeTrue();

        manager.Progress.Should().Be(new SessionProgress(
            TimerText: "00:16",
            InstructionText: ExerciseStep.GetDefaultText(BreathingPhaseType.HoldIn),
            StepSecondsText: "1",
            Phase: BreathingPhaseType.HoldIn,
            StepIndex: 1,
            StepCount: 4,
            CycleIndex: 0,
            Cycles: 3));
        reported.Should().Equal(manager.Progress);
        manager.BreathingCircleSize.Should().Be(MaxSize);
        circleSizes.Should().Equal(MaxSize);
    }

    [AvaloniaFact]
    public void StepBackward_JumpsToThePreviousStepAcrossCycles()
    {
        var manager = CreateManager(3);
        var reported = 0;
        manager.ProgressChanged += (_, _) => reported++;

        manager.StepBackward().Should().BeFalse();
        reported.Should().Be(0);

        for (var i = 0; i < 4; i++) manager.StepForward();
        manager.Progress.CycleIndex.Should().Be(1);
        manager.Progress.StepIndex.Should().Be(0);

        manager.StepBackward().Should().BeTrue();

        manager.Progress.CycleIndex.Should().Be(0);
        manager.Progress.StepIndex.Should().Be(3);
        manager.Progress.Phase.Should().Be(BreathingPhaseType.HoldOut);
        manager.Progress.StepSecondsText.Should().Be("1");
        manager.BreathingCircleSize.Should().Be(MinSize);
        reported.Should().Be(5);
    }

    [AvaloniaFact]
    public void Restart_ReturnsToTheFirstStepAndResetsTheElapsedTime()
    {
        var manager = CreateManager(3);
        manager.StartSession();
        RunDispatcherFor(TimeSpan.FromSeconds(1.2));
        for (var i = 0; i < 6; i++) manager.StepForward();
        manager.ElapsedTime.Should().BePositive();
        var reported = new List<SessionProgress>();
        manager.ProgressChanged += (_, progress) => reported.Add(progress);

        manager.Restart();

        manager.ElapsedTime.Should().Be(TimeSpan.Zero);
        manager.Progress.StepIndex.Should().Be(0);
        manager.Progress.CycleIndex.Should().Be(0);
        manager.Progress.StepSecondsText.Should().Be("2");
        manager.Progress.TimerText.Should().Be("00:18");
        manager.BreathingCircleSize.Should().Be(MinSize);
        manager.IsPaused.Should().BeFalse();
        reported.Should().Equal(manager.Progress);

        manager.StopSession();
    }

    [AvaloniaTheory]
    [InlineData(0, MinSize)] // inhale starts small
    [InlineData(1, MaxSize)] // holding in stays large
    [InlineData(2, MaxSize)] // exhale starts large
    [InlineData(3, MinSize)] // holding out stays small
    [InlineData(4, MinSize)] // inhale of the next cycle
    public void StepJumps_SnapTheCircleToWhereThePhaseStarts(int jumps, double expectedSize)
    {
        var manager = CreateManager(3);

        // jumping past the target and back checks both directions
        for (var i = 0; i < jumps + 1; i++) manager.StepForward();
        manager.StepBackward();

        manager.Progress.StepIndex.Should().Be(jumps % BoxSteps.Count);
        manager.BreathingCircleSize.Should().Be(expectedSize);
    }

    [AvaloniaFact]
    public void StepForward_WithoutCycles_LoopsWithoutFinishing()
    {
        var manager = CreateManager(null);
        var finished = false;
        manager.TimerFinished += (_, _) => finished = true;

        for (var i = 0; i < 12; i++)
        {
            manager.StepForward().Should().BeTrue();
        }

        finished.Should().BeFalse();
        manager.Progress.CycleIndex.Should().Be(3);
        manager.Progress.StepIndex.Should().Be(0);
        manager.Progress.Cycles.Should().BeNull();
        manager.CompletedCycles.Should().Be(3);

        // an endless session shows the elapsed position instead of a countdown
        manager.Progress.TimerText.Should().Be("00:18");
    }

    [AvaloniaFact]
    public void StepForward_PastTheLastStep_FinishesTheSession()
    {
        var manager = CreateManager(1);
        var finished = 0;
        manager.TimerFinished += (_, _) => finished++;
        manager.StartSession();

        for (var i = 0; i < 3; i++) manager.StepForward();
        finished.Should().Be(0);

        manager.StepForward().Should().BeTrue();

        finished.Should().Be(1);
        manager.CompletedCycles.Should().Be(1);
        manager.Progress.StepSecondsText.Should().Be("0");
        manager.StepForward().Should().BeFalse();
        finished.Should().Be(1);
    }

    [AvaloniaFact]
    public void RunningSession_TicksEverySecond()
    {
        var manager = CreateManager(3);
        var reported = 0;
        manager.ProgressChanged += (_, _) => reported++;

        manager.StartSession();
        RunDispatcherFor(TimeSpan.FromSeconds(1.3));

        manager.ElapsedTime.Should().BeGreaterThanOrEqualTo(TimeSpan.FromSeconds(1));
        reported.Should().BeGreaterThan(1);

        manager.StopSession();
    }

    [AvaloniaFact]
    public void Jumps_WhilePaused_ReportTheNewStepWithoutStartingTheTimers()
    {
        var manager = CreateManager(3);
        manager.StartSession();
        manager.PauseSession();
        var reported = new List<SessionProgress>();
        var circleSizes = new List<double>();
        manager.ProgressChanged += (_, progress) => reported.Add(progress);
        manager.BreathingCircleAnimationTimerTick += (_, size) => circleSizes.Add(size);

        manager.StepForward();
        manager.StepForward();
        manager.StepBackward();
        manager.Restart();
        manager.StepForward();

        reported.Select(p => p.StepIndex).Should().Equal(1, 2, 1, 0, 1);
        circleSizes.Should().Equal(MaxSize, MaxSize, MaxSize, MinSize, MaxSize);

        RunDispatcherFor(TimeSpan.FromSeconds(1.3));

        manager.IsPaused.Should().BeTrue();
        manager.ElapsedTime.Should().Be(TimeSpan.Zero);
        manager.Progress.StepSecondsText.Should().Be("1");
        reported.Should().HaveCount(5);
        circleSizes.Should().HaveCount(5);

        manager.StopSession();
    }
}
