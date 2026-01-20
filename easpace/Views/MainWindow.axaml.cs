using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace easpace.Views;

public partial class MainWindow : Window
{
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
    }
}