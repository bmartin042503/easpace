// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Services.Core;
using easpace.Desktop.ValidationAttributes;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Wellness.ViewModels;

/// <summary>
/// Represents an editable instruction of the exercise form.
/// </summary>
internal partial class ExerciseInstructionViewModel : ValidatorViewModelBase
{
    private static readonly IReadOnlyList<PhaseOption> AllPhaseOptions =
    [
        new(null, LocalizationService.GetString("Wellness.Phase.None")),
        new(BreathingPhaseType.Inhale, LocalizationService.GetString("Wellness.Phase.Inhale")),
        new(BreathingPhaseType.HoldIn, LocalizationService.GetString("Wellness.Phase.HoldIn")),
        new(BreathingPhaseType.Exhale, LocalizationService.GetString("Wellness.Phase.Exhale")),
        new(BreathingPhaseType.HoldOut, LocalizationService.GetString("Wellness.Phase.HoldOut"))
    ];

    /// <summary>
    /// Gets the 1-based position of the instruction within its cycle.
    /// </summary>
    [ObservableProperty] private int _number;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(256, ErrorMessage = "FormValidation.InstructionText.MaxLength")]
    [RequiredIf(nameof(Phase), null, ErrorMessage = "FormValidation.InstructionText.Required")]
    private string _text;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(1, 3600, ErrorMessage = "FormValidation.Duration.Range")]
    private int _durationSeconds;

    private PhaseOption _selectedPhase;

    /// <summary>
    /// Gets or sets whether the preview is currently playing this instruction.
    /// </summary>
    [ObservableProperty] private bool _isPreviewing;

    /// <summary>
    /// Gets whether the instruction can have a breathing phase, which only breathing exercises allow.
    /// </summary>
    public bool SupportsPhases { get; }

    public IReadOnlyList<PhaseOption> PhaseOptions => AllPhaseOptions;

    /// <summary>
    /// Gets or sets the chosen phase option. A <c>null</c> value is ignored: the selector sends it when its row is
    /// torn down, which isn't a choice of the user.
    /// </summary>
    public PhaseOption? SelectedPhase
    {
        get => _selectedPhase;
        set
        {
            if (value is null || !SetProperty(ref _selectedPhase, value)) return;

            OnPropertyChanged(nameof(Phase));
            OnPropertyChanged(nameof(TextPlaceholder));

            // the text is only required without a phase
            ValidateProperty(Text, nameof(Text));
        }
    }

    public BreathingPhaseType? Phase => _selectedPhase.Value;

    /// <summary>
    /// Gets the hint of the empty text box. With a phase, it's the phase's default text, which is used while the text is empty.
    /// </summary>
    public string TextPlaceholder => Phase is null
        ? LocalizationService.GetString("Wellness.Editor.Input.TextPlaceholder")
        : ExerciseStep.GetDefaultText(Phase);

    /// <summary>
    /// Initializes a new instance of the <see cref="ExerciseInstructionViewModel"/> class.
    /// </summary>
    /// <param name="supportsPhases">Whether the instruction may have a phase; otherwise the phase is dropped.</param>
    public ExerciseInstructionViewModel(string text, int durationSeconds, BreathingPhaseType? phase, bool supportsPhases)
    {
        SupportsPhases = supportsPhases;

        _text = text;
        _durationSeconds = durationSeconds;
        _selectedPhase = AllPhaseOptions.Single(o => o.Value == (supportsPhases ? phase : null));

        ValidateAllProperties();
    }

    /// <summary>
    /// Validates every field, so their errors are shown.
    /// </summary>
    public void Validate() => ValidateAllProperties();
}

/// <summary>
/// Represents a choice of the phase selector, where "no phase" has a <c>null</c> value.
/// </summary>
/// <param name="Value">The breathing phase, or <c>null</c> for an instruction without phase.</param>
/// <param name="Label">The localized name of the choice.</param>
internal sealed record PhaseOption(BreathingPhaseType? Value, string Label);
