// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using easpace.Desktop.Constants;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Factories;

public class PageFactory(Func<ApplicationPage, PageViewModel> factory)
{
    public PageViewModel GetPageViewModel(ApplicationPage page) => factory.Invoke(page);
}