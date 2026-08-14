// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Models;

namespace easpace.Desktop.ViewModels;

public partial class WellnessSessionViewModel : ViewModelBase
{
    private WellnessSession _session;
    
    public WellnessSessionViewModel(WellnessSession session)
    {
        _session = session;
    }
}