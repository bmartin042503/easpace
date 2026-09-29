// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Services;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Presentation;
using easpace.Desktop.ViewModels;
using easpace.Desktop.ViewModels.Dialogs;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Wellness.ViewModels;

internal partial class WellnessEndingViewModel : ViewModelBase
{
    private readonly IWellnessSessionEntryService _sessionEntryService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<WellnessEndingViewModel> _logger;

    private CreateWellnessSessionEntryRequest _createEntryRequest;

    public event EventHandler? NavigatedToConfiguration;

    [ObservableProperty] private bool _askToSaveSession;
    [ObservableProperty] private bool _sessionSaved;
    [ObservableProperty] private string _titleText = string.Empty;

    [ObservableProperty] private string _durationText = string.Empty;
    [ObservableProperty] private WellnessSessionType _sessionType;
    [ObservableProperty] private string _exerciseName = string.Empty;

    // null for exercises that run once, as cycles only mean something when the exercise repeats
    [ObservableProperty] private int? _cycleCount;

    public WellnessEndingViewModel(
        IWellnessSessionEntryService sessionEntryService,
        IDialogService dialogService,
        CreateWellnessSessionEntryRequest createWellnessSessionEntryRequest,
        ILogger<WellnessEndingViewModel> logger)
    {
        _sessionEntryService = sessionEntryService;
        _dialogService = dialogService;
        _logger = logger;
        _createEntryRequest = createWellnessSessionEntryRequest;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        try
        {
            if (_createEntryRequest.ActualDuration == _createEntryRequest.TargetDuration)
            {
                await SaveSession();
            }
            else
            {
                TitleText = LocalizationService.GetString("Wellness.Question.SaveSession");
                AskToSaveSession = true;
            }

            DurationText =
                _createEntryRequest.ActualDuration.ToString(_createEntryRequest.ActualDuration.TotalHours >= 1
                    ? @"hh\:mm\:ss"
                    : @"mm\:ss");

            SessionType = _createEntryRequest.SessionType;
            ExerciseName = _createEntryRequest.ExerciseName ?? string.Empty;
            CycleCount = _createEntryRequest.CompletedCycles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize wellness ending view");
        }
    }

    [RelayCommand]
    private void NavigateBack()
    {
        NavigatedToConfiguration?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task SaveSession()
    {
        AskToSaveSession = false;

        try
        {
            _logger.LogInformation("Saving completed wellness session to database");
            await _sessionEntryService.CreateWellnessSessionEntryAsync(_createEntryRequest);
            SessionSaved = true;
            TitleText = LocalizationService.GetString("Wellness.Text.SessionSaved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save completed wellness session");
            
            var errorDialog = new ErrorDialogViewModel
            {
                Title = LocalizationService.GetString("Common.Error.Title"),
                Message = LocalizationService.GetString("Wellness.Error.SaveFailed")
            };
            
            await _dialogService.ShowDialogAsync(errorDialog);
        }
    }
}