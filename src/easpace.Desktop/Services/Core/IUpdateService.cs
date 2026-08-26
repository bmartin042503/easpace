// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Threading.Tasks;

namespace easpace.Desktop.Services.Core;

internal interface IUpdateService
{
    Task<UpdateCheckResult> CheckForUpdatesAsync();
}