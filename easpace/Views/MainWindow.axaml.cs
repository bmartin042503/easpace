using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace easpace.Views;

public partial class MainWindow : Window
{
    private Button _previousSelectedButton;
    public MainWindow()
    {
        InitializeComponent();
        
        // TODO: implement custom window manager buttons for Windows and Linux
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows specific settings

            // Remove title bar
            ExtendClientAreaToDecorationsHint = true;
            ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome;
            ExtendClientAreaTitleBarHeightHint = 0;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Remove title bar on macOS, but keep window control buttons in the top left corner
            ExtendClientAreaToDecorationsHint = true;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            
        }

        // sets Mood button as selected by default
        _previousSelectedButton = MoodButton;
        _previousSelectedButton.Classes.Add("selected");
    }
    
    private void SidebarButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button clickedButton) return;
        
        // add and remove 'selected' button styles
        _previousSelectedButton.Classes.Remove("selected");
        clickedButton.Classes.Add("selected");
        _previousSelectedButton = clickedButton;
    }
}
