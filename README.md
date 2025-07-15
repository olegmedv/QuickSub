# QuickSub - Lightweight Subtitle Overlay

QuickSub is a lightweight .NET 8 application that provides real-time subtitle overlay functionality by integrating with Windows Live Captions. It offers automatic translation capabilities and creates an unobtrusive transparent overlay that remains hidden from screen recordings and captures.

## Features

- **Live Caption Integration**: Automatically captures captions from Windows Live Captions service
- **Real-time Translation**: Supports translation to multiple languages using Google Translate API
- **Transparent Overlay**: Non-intrusive subtitle display that doesn't interfere with applications
- **Screen Capture Protection**: Automatically hides subtitles from screen recordings and sharing
- **System Tray Integration**: Minimizes to system tray with easy access controls
- **Customizable Appearance**: Configurable fonts, colors, and positioning

## System Requirements

- Windows 11 (x64, x86, ARM64)
- Windows Live Captions service enabled
- Internet connection (for translation features)
- No .NET runtime required (self-contained executable)

## Quick Start

### Basic Usage

1. **Overlay Mode** (Default):

   ```bash
   QuickSub.exe
   ```

2. **Console Mode** (for debugging):
   ```bash
   QuickSub.exe --console
   ```

### Building from Source

Choose the appropriate build script based on your needs:

```bash
# Recommended: Compact build with maximum compression (~50-70MB)
./build.bat

# Conservative build without trimming (~60-80MB)
# Use PowerShell with -ConservativeMode parameter

# Safe build with full compatibility (~70-100MB)
# Use PowerShell with -SafeMode parameter

# PowerShell builds with options
./build.ps1                    # Default compact
./build.ps1 -ConservativeMode  # Conservative
./build.ps1 -SafeMode          # Safe mode
./build.ps1 -NoTrimming        # Without trimming

# Available build scripts:
# - build.bat (wrapper for PowerShell builds)
# - build.ps1 (main PowerShell build script)
```

## Architecture

### Technology Stack

- **.NET 8** with Windows-specific APIs
- **WPF + Windows Forms** hybrid architecture
- **UI Automation** for Live Captions integration
- **HTTP client** for translation services

### Core Components

#### Program.cs

Entry point supporting dual execution modes with command-line argument parsing.

#### SubtitleModel (Models/SubtitleModel.cs)

MVVM singleton model managing caption state, automatic translation, and text processing pipeline.

#### OverlayWindow (Views/OverlayWindow.xaml)

Transparent WPF window with:

- Custom resize handles and positioning
- Screen capture hiding integration
- Real-time subtitle display
- Automatic text scaling and formatting

#### TrayIcon (Services/TrayIcon.cs)

Windows Forms system tray integration providing:

- Application control and settings access
- Quick toggle for translation features
- Exit and minimize functionality

#### WindowHidingService (Services/WindowHidingService.cs)

Prevents subtitle overlay from appearing in:

- Screen recordings
- Video conferencing applications
- Screenshot captures
- Screen sharing sessions

#### Translation Pipeline

- **SubtitleTranslator** (Services/SubtitleTranslator.cs): Asynchronous translation with similarity checking
- **SubtitleTextHelper** (Helpers/SubtitleTextHelper.cs): Text preprocessing and cleanup utilities

### Live Captions Integration

The application uses UI Automation to hook into Windows Live Captions:

```csharp
// Polling loop for caption updates (25ms interval)
AutomationElement captionsElement = AutomationElement.FindFirst(
    AutomationElement.RootElement,
    new PropertyCondition(AutomationElement.AutomationIdProperty, "CaptionsTextBlock")
);
```

**Requirements:**

- Windows Live Captions service must be enabled
- Accessibility permissions for UI Automation
- Windows 11 22H2+ (Windows 10 support may be limited)

## Configuration

### Settings Management

Settings are automatically saved to `settings.json` with the following categories:

#### Display Settings

- Font family, size, and styling
- Text colors and transparency
- Window position and sizing
- Display mode preferences

#### Translation Settings

- Target language selection
- Translation service configuration
- Automatic translation toggle
- Text similarity thresholds

#### Performance Settings

- Polling interval for Live Captions
- Translation caching preferences
- Screen hiding behavior

### Example Configuration

```json
{
  "FontFamily": "Segoe UI",
  "FontSize": 16,
  "TextColor": "#FFFFFF",
  "BackgroundColor": "#80000000",
  "TargetLanguage": "ru",
  "AutoTranslate": true,
  "WindowPosition": { "X": 100, "Y": 100 },
  "Transparency": 0.8
}
```

## Build System

### Single-File Deployment

All builds create self-contained executables with embedded dependencies:

- **Compact Build**: Soft trimming with WPF/WinForms protection (~50-70MB)
- **Conservative Build**: Compression without aggressive trimming (~60-80MB)
- **Safe Build**: Full size with maximum compatibility (~70-100MB)

### Trimming Protection

The project includes comprehensive assembly protection:

```xml
<TrimmerRootAssembly Include="WindowsBase" />
<TrimmerRootAssembly Include="PresentationCore" />
<TrimmerRootAssembly Include="PresentationFramework" />
<TrimmerRootAssembly Include="System.Windows.Forms" />
```

### Windows Forms Compatibility

- Uses `PublishTrimmed=false` to resolve NETSDK1175 errors
- Comprehensive `ILLink.Descriptors.xml` for type preservation
- Safe binary formatter serialization support

## Development

### Project Structure

```
QuickSub/
├── App.xaml(.cs)              # WPF application definition
├── Program.cs                 # Entry point and mode selection
├── Models/
│   ├── QuickSubSettings.cs    # Configuration management
│   └── SubtitleModel.cs       # Core MVVM model
├── Views/
│   ├── OverlayWindow.xaml(.cs)    # Main overlay UI
│   └── SettingsWindow.xaml(.cs)   # Configuration interface
├── Services/
│   ├── TrayIcon.cs                # System tray integration
│   ├── WindowHidingService.cs     # Screen capture protection
│   └── SubtitleTranslator.cs      # Translation pipeline
├── Helpers/
│   ├── IconHelper.cs              # Icon and resource management
│   ├── OverlayWindowHelper.cs     # Window management helpers
│   └── SubtitleTextHelper.cs      # Text processing utilities
├── Converters/
│   └── BoolToVisibilityConverter.cs # XAML value converter
├── ILLink.Descriptors.xml     # Trimming protection
├── QuickSub.csproj           # Project configuration
├── QuickSub.sln              # Solution file
├── build.bat                 # Build script
└── build.ps1                 # PowerShell build script
```

### Common Issues and Solutions

#### Live Captions Not Found

- Verify Windows Live Captions is enabled in Settings > Accessibility
- Check Windows version compatibility (11 22H2+ recommended)
- Ensure UI Automation permissions are granted

#### Translation Failures

- Verify internet connectivity
- Check Google Translate API rate limits
- Review translation service configuration

#### Build Errors (NETSDK1175)

- Project configured with `PublishTrimmed=false`
- Comprehensive assembly protection included
- Use provided build scripts for best results

#### Performance Issues

- Adjust polling interval in settings
- Disable translation for better performance
- Use console mode for debugging

### Testing

Manual testing focuses on:

- Live Captions integration accuracy
- Translation service reliability
- Screen capture hiding functionality
- UI responsiveness and memory usage

## Distribution

### Deployment

- **Single-file executables** with no installation required
- **Portable applications** - copy and run anywhere
- **No .NET runtime** installation needed
- **Cross-architecture support** (x64, x86, ARM64)

### Security Notes

- Uses Windows APIs for system integration
- HTTP clients for translation services only
- No sensitive data storage in application code
- Screen capture protection for privacy

## Inspiration

This project was inspired by [SakiRinn/LiveCaptions-Translator](https://github.com/SakiRinn/LiveCaptions-Translator).

## License

This project is designed for educational and personal use. Review applicable terms for Google Translate API usage and Windows Live Captions integration requirements.
