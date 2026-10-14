// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Threading.Tasks;
using easpace.Desktop.Constants;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Data;
using Microsoft.EntityFrameworkCore;

namespace easpace.Desktop.Data;

internal class DbSeeder
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IPreferencesService _preferencesService;

    public DbSeeder(IDbContextFactory<AppDbContext> dbContextFactory, IPreferencesService preferencesService)
    {
        _dbContextFactory = dbContextFactory;
        _preferencesService = preferencesService;
    }

    public async Task SeedAsync()
    {
        // seed only once, so exercises deleted by the user don't come back on the next launch
        if (_preferencesService.ReadPreference<bool>(PreferenceKey.DefaultWellnessExercisesSeeded)) return;

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        // Cyclic Sighing
        // https://med.stanford.edu/news/insights/2023/02/cyclic-sighing-can-help-breathe-away-anxiety.html
        var cyclicSighing = new WellnessExercise
        {
            Name = LocalizationService.GetString("Wellness.CyclicSighing.Name"),
            Description = LocalizationService.GetString("Wellness.CyclicSighing.Description"),
            DefaultCycleCount = 25,
            Instructions =
            [
                new WellnessExerciseInstruction
                {
                    Order = 0,
                    Text = LocalizationService.GetString("Wellness.CyclicSighing.Instruction1"),
                    DurationSeconds = 3,
                    BreathingPhase = BreathingPhase.Inhale
                },
                new WellnessExerciseInstruction
                {
                    Order = 1,
                    Text = LocalizationService.GetString("Wellness.CyclicSighing.Instruction2"),
                    DurationSeconds = 2,
                    BreathingPhase = BreathingPhase.Inhale
                },
                new WellnessExerciseInstruction
                {
                    Order = 2,
                    Text = LocalizationService.GetString("Wellness.CyclicSighing.Instruction3"),
                    DurationSeconds = 7,
                    BreathingPhase = BreathingPhase.Exhale
                }
            ]
        };

        // Box Breathing
        // https://health.clevelandclinic.org/box-breathing-benefits
        var boxBreathing = new WellnessExercise
        {
            Name = LocalizationService.GetString("Wellness.BoxBreathing.Name"),
            Description = LocalizationService.GetString("Wellness.BoxBreathing.Description"),
            DefaultCycleCount = 12,
            Instructions =
            [
                new WellnessExerciseInstruction
                {
                    Order = 0,
                    Text = LocalizationService.GetString("Wellness.BoxBreathing.Instruction1"),
                    DurationSeconds = 4,
                    BreathingPhase = BreathingPhase.Inhale
                },
                new WellnessExerciseInstruction
                {
                    Order = 1,
                    Text = LocalizationService.GetString("Wellness.BoxBreathing.Instruction2"),
                    DurationSeconds = 4,
                    BreathingPhase = BreathingPhase.HoldIn
                },
                new WellnessExerciseInstruction
                {
                    Order = 2,
                    Text = LocalizationService.GetString("Wellness.BoxBreathing.Instruction3"),
                    DurationSeconds = 4,
                    BreathingPhase = BreathingPhase.Exhale
                },
                new WellnessExerciseInstruction
                {
                    Order = 3,
                    Text = LocalizationService.GetString("Wellness.BoxBreathing.Instruction4"),
                    DurationSeconds = 4,
                    BreathingPhase = BreathingPhase.HoldOut
                }
            ]
        };
        
        // Sensory Grounding
        // https://www.urmc.rochester.edu/behavioral-health-partners/bhp-blog/april-2018/5-4-3-2-1-coping-technique-for-anxiety
        var sensoryGrounding = new WellnessExercise
        {
            Name = LocalizationService.GetString("Wellness.SensoryGrounding.Name"),
            Description = LocalizationService.GetString("Wellness.SensoryGrounding.Description"),
            DefaultCycleCount = 1,
            Instructions =
            [
                new WellnessExerciseInstruction
                {
                    Order = 0,
                    Text = LocalizationService.GetString("Wellness.SensoryGrounding.Instruction1"),
                    DurationSeconds = 4,
                    BreathingPhase = BreathingPhase.Inhale
                },
                new WellnessExerciseInstruction
                {
                    Order = 1,
                    Text = LocalizationService.GetString("Wellness.SensoryGrounding.Instruction2"),
                    DurationSeconds = 6,
                    BreathingPhase = BreathingPhase.Exhale
                },
                new WellnessExerciseInstruction
                {
                    Order = 2,
                    Text = LocalizationService.GetString("Wellness.SensoryGrounding.Instruction3"),
                    DurationSeconds = 30
                },
                new WellnessExerciseInstruction
                {
                    Order = 3,
                    Text = LocalizationService.GetString("Wellness.SensoryGrounding.Instruction4"),
                    DurationSeconds = 25
                },
                new WellnessExerciseInstruction
                {
                    Order = 4,
                    Text = LocalizationService.GetString("Wellness.SensoryGrounding.Instruction5"),
                    DurationSeconds = 25
                },
                new WellnessExerciseInstruction
                {
                    Order = 5,
                    Text = LocalizationService.GetString("Wellness.SensoryGrounding.Instruction6"),
                    DurationSeconds = 20
                },
                new WellnessExerciseInstruction
                {
                    Order = 6,
                    Text = LocalizationService.GetString("Wellness.SensoryGrounding.Instruction7"),
                    DurationSeconds = 15
                },
                new WellnessExerciseInstruction
                {
                    Order = 7,
                    Text = LocalizationService.GetString("Wellness.SensoryGrounding.Instruction8"),
                    DurationSeconds = 4,
                    BreathingPhase = BreathingPhase.Inhale
                },
                new WellnessExerciseInstruction
                {
                    Order = 8,
                    Text = LocalizationService.GetString("Wellness.SensoryGrounding.Instruction9"),
                    DurationSeconds = 6,
                    BreathingPhase = BreathingPhase.Exhale
                },
                new WellnessExerciseInstruction
                {
                    Order = 9,
                    Text = LocalizationService.GetString("Wellness.SensoryGrounding.Instruction10"),
                    DurationSeconds = 15
                }
            ]
        };

        // Body Scan
        // https://newsinhealth.nih.gov/2021/06/mindfulness-your-health
        var bodyScan = new WellnessExercise
        {
            Name = LocalizationService.GetString("Wellness.BodyScan.Name"),
            Description = LocalizationService.GetString("Wellness.BodyScan.Description"),
            DefaultCycleCount = 1,
            Instructions =
            [
                new WellnessExerciseInstruction
                {
                    Order = 0,
                    Text = LocalizationService.GetString("Wellness.BodyScan.Instruction1"),
                    DurationSeconds = 30
                },
                new WellnessExerciseInstruction
                {
                    Order = 1,
                    Text = LocalizationService.GetString("Wellness.BodyScan.Instruction2"),
                    DurationSeconds = 30
                },
                new WellnessExerciseInstruction
                {
                    Order = 2,
                    Text = LocalizationService.GetString("Wellness.BodyScan.Instruction3"),
                    DurationSeconds = 30
                },
                new WellnessExerciseInstruction
                {
                    Order = 3,
                    Text = LocalizationService.GetString("Wellness.BodyScan.Instruction4"),
                    DurationSeconds = 30
                },
                new WellnessExerciseInstruction
                {
                    Order = 4,
                    Text = LocalizationService.GetString("Wellness.BodyScan.Instruction5"),
                    DurationSeconds = 30
                },
                new WellnessExerciseInstruction
                {
                    Order = 5,
                    Text = LocalizationService.GetString("Wellness.BodyScan.Instruction6"),
                    DurationSeconds = 30
                },
                new WellnessExerciseInstruction
                {
                    Order = 6,
                    Text = LocalizationService.GetString("Wellness.BodyScan.Instruction7"),
                    DurationSeconds = 30
                },
                new WellnessExerciseInstruction
                {
                    Order = 7,
                    Text = LocalizationService.GetString("Wellness.BodyScan.Instruction8"),
                    DurationSeconds = 30
                }
            ]
        };

        dbContext.WellnessExercises.AddRange(cyclicSighing, boxBreathing, sensoryGrounding, bodyScan);
        await dbContext.SaveChangesAsync();

        _preferencesService.SavePreference(PreferenceKey.DefaultWellnessExercisesSeeded, true);
    }
}
