// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

namespace easpace.Desktop.Validation;

internal sealed record ValidationIssue(string MemberName, string MessageKey);