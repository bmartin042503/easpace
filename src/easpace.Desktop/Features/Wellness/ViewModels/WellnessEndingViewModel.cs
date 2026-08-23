// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Features.Wellness.Services;
using easpace.Desktop.Services;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Wellness.ViewModels;

public partial class WellnessEndingViewModel : ViewModelBase
{
    private readonly IWellnessSessionEntryService _sessionEntryService;

    private CreateWellnessSessionEntryRequest _createEntryRequest;

    public event EventHandler? NavigatedToConfiguration;

    [ObservableProperty] private bool _askToSaveSession;
    [ObservableProperty] private bool _sessionSaved;
    [ObservableProperty] private string _titleText = string.Empty;

    [ObservableProperty] private string _durationText = string.Empty;
    [ObservableProperty] private WellnessSessionType _sessionType;
    [ObservableProperty] private bool _isBreathingType;
    [ObservableProperty] private string _breathingTechniqueName = string.Empty;
    [ObservableProperty] private int _cycleCount;

    public WellnessEndingViewModel(
        IWellnessSessionEntryService sessionEntryService,
        CreateWellnessSessionEntryRequest createWellnessSessionEntryRequest)
    {
        _sessionEntryService = sessionEntryService;
        _createEntryRequest = createWellnessSessionEntryRequest;
    }

    [RelayCommand]
    public async Task InitializeAsync()
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

        IsBreathingType = SessionType == WellnessSessionType.Breathing;

        if (IsBreathingType && _createEntryRequest.BreathingTechnique != null)
        {
            BreathingTechniqueName = _createEntryRequest.BreathingTechnique.IsLocalized
                ? LocalizationService.GetString(_createEntryRequest.BreathingTechnique.Name)
                : _createEntryRequest.BreathingTechnique.Name;

            CycleCount = _createEntryRequest.ActualDuration.Seconds /
                         _createEntryRequest.BreathingTechnique.Phases.Sum(p => p.DurationSeconds);
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

        await _sessionEntryService.CreateWellnessSessionEntryAsync(_createEntryRequest);

        SessionSaved = true;

        TitleText = LocalizationService.GetString("Wellness.Text.SessionSaved");
    }
}