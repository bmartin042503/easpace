using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using easpace.Desktop.ViewModels.Activities;

namespace easpace.Desktop.DataTemplates;

public class ActivityViewModelTemplateSelector : IDataTemplate
{
    [Content]
    public Dictionary<string, IDataTemplate> AvailableTemplates { get; } = new();

    public Control? Build(object? param)
    {
        if (param is not ActivityViewModel activityViewModel)
        {
            return null;
        }

        var key = activityViewModel.GetType().Name;

        if (string.IsNullOrEmpty(key)) return null;

        return AvailableTemplates.TryGetValue(key, out var template) ? template.Build(param) : null;
    }

    public bool Match(object? data)
    {
        if (data is not ActivityViewModel activityViewModel) return false;
        var key = activityViewModel.GetType().Name;
        return !string.IsNullOrEmpty(key) && AvailableTemplates.ContainsKey(key);
    }
}