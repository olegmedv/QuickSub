using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuickSub
{
    public class QuickSubSettings
    {
        private static QuickSubSettings? _instance;
        private static readonly string SettingsFilePath = GetSettingsFilePath();

        private static string GetSettingsFilePath()
        {
            // Try executable directory first for backward compatibility
            var executableDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(executableDir))
            {
                var executablePath = Path.Combine(executableDir, "settings.json");
                try
                {
                    // Test if we can write to this directory
                    var testFile = Path.Combine(executableDir, $"test_write_{Guid.NewGuid()}.tmp");
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                    return executablePath;
                }
                catch
                {
                    // Can't write to executable directory, fall back to AppData
                }
            }
            
            // Fallback to AppData for single-file deployment or protected directories
            var appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "QuickSub");
            
            if (!Directory.Exists(appDataDir))
            {
                Directory.CreateDirectory(appDataDir);
            }
            
            return Path.Combine(appDataDir, "settings.json");
        }

        public static event EventHandler? SettingsChanged;

        public static QuickSubSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    System.Diagnostics.Debug.WriteLine("Creating new QuickSubSettings instance");
                    System.Diagnostics.Debug.WriteLine($"Settings file path: {SettingsFilePath}");
                    _instance = Load();
                }
                return _instance;
            }
        }

        public string TargetLanguage { get; set; } = "";
        public int FontSize { get; set; } = 0;
        public bool ShowOriginal { get; set; } = false;
        public bool ShowTranslation { get; set; } = false;
        public int ColorIndex { get; set; } = 0;
        public int BackgroundColorIndex { get; set; } = 0;
        public byte Opacity { get; set; } = 0;
        public int DisplayMode { get; set; } = 0; // 0 = both, 1 = translation only, 2 = original only
        

        public QuickSubSettings() { }

        public void Save(bool notifyChange = true)
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsFilePath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsFilePath, json);
                
                System.Diagnostics.Debug.WriteLine($"Settings saved to {SettingsFilePath}: {json}");
                
                // Notify about settings changes
                if (notifyChange)
                {
                    SettingsChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Settings save error: {ex.Message}");
            }
        }

        public static QuickSubSettings Load()
        {
            System.Diagnostics.Debug.WriteLine($"Load() called, checking file: {SettingsFilePath}");
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Settings file exists: {SettingsFilePath}");
                    var json = File.ReadAllText(SettingsFilePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        System.Diagnostics.Debug.WriteLine($"JSON content: {json}");
                        var settings = JsonSerializer.Deserialize<QuickSubSettings>(json);
                        if (settings != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Deserialized settings - FontSize: {settings.FontSize}, ColorIndex: {settings.ColorIndex}, BackgroundColorIndex: {settings.BackgroundColorIndex}, Opacity: {settings.Opacity}, DisplayMode: {settings.DisplayMode}");
                            
                            // Fill with defaults if something is invalid
                            var defaults = CreateDefaultSettings();
                            bool needsFix = false;
                            
                            if (string.IsNullOrWhiteSpace(settings.TargetLanguage)) {
                                settings.TargetLanguage = defaults.TargetLanguage;
                                needsFix = true;
                                System.Diagnostics.Debug.WriteLine("Fixed TargetLanguage");
                            }
                            if (settings.FontSize <= 0) {
                                settings.FontSize = defaults.FontSize;
                                needsFix = true;
                                System.Diagnostics.Debug.WriteLine("Fixed FontSize");
                            }
                            if (settings.ColorIndex <= 0 || settings.ColorIndex > 8) {
                                settings.ColorIndex = defaults.ColorIndex;
                                needsFix = true;
                                System.Diagnostics.Debug.WriteLine("Fixed ColorIndex");
                            }
                            if (settings.BackgroundColorIndex <= 0 || settings.BackgroundColorIndex > 8) {
                                settings.BackgroundColorIndex = defaults.BackgroundColorIndex;
                                needsFix = true;
                                System.Diagnostics.Debug.WriteLine("Fixed BackgroundColorIndex");
                            }
                            if (settings.Opacity < 1 || settings.Opacity > 255) {
                                settings.Opacity = defaults.Opacity;
                                needsFix = true;
                                System.Diagnostics.Debug.WriteLine("Fixed Opacity");
                            }
                            if (settings.DisplayMode < 0 || settings.DisplayMode > 2) {
                                settings.DisplayMode = defaults.DisplayMode;
                                needsFix = true;
                                System.Diagnostics.Debug.WriteLine("Fixed DisplayMode");
                            }
                            
                            if (needsFix) {
                                System.Diagnostics.Debug.WriteLine("Some settings were invalid and replaced with defaults");
                            }
                            
                            System.Diagnostics.Debug.WriteLine($"Final settings - FontSize: {settings.FontSize}, ColorIndex: {settings.ColorIndex}, BackgroundColorIndex: {settings.BackgroundColorIndex}, Opacity: {settings.Opacity}, DisplayMode: {settings.DisplayMode}");
                            return settings;
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"Settings file not found or invalid at {SettingsFilePath}, creating with defaults");
                var defaultSettings = CreateDefaultSettings();
                // Save default settings to file only if file doesn't exist
                if (!File.Exists(SettingsFilePath))
                {
                    defaultSettings.Save(false); // Don't notify when creating defaults
                }
                return defaultSettings;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Settings load error: {ex.Message}");
                var defaultSettings = CreateDefaultSettings();
                // Save default settings to file only if file doesn't exist
                if (!File.Exists(SettingsFilePath))
                {
                    defaultSettings.Save(false); // Don't notify when creating defaults
                }
                return defaultSettings;
            }
        }

        private static QuickSubSettings CreateDefaultSettings()
        {
            return new QuickSubSettings
            {
                TargetLanguage = "ru",
                FontSize = 18,
                ShowOriginal = true,
                ShowTranslation = true,
                ColorIndex = 1,
                BackgroundColorIndex = 8,
                Opacity = 150,
                DisplayMode = 0,
            };
        }

        public void ResetToDefaults()
        {
            var defaults = CreateDefaultSettings();
            TargetLanguage = defaults.TargetLanguage;
            FontSize = defaults.FontSize;
            ShowOriginal = defaults.ShowOriginal;
            ShowTranslation = defaults.ShowTranslation;
            ColorIndex = defaults.ColorIndex;
            BackgroundColorIndex = defaults.BackgroundColorIndex;
            Opacity = defaults.Opacity;
            DisplayMode = defaults.DisplayMode;
        }
    }
}