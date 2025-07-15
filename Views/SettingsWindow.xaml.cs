using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfComboBoxItem = System.Windows.Controls.ComboBoxItem;
using WpfMessageBox = System.Windows.MessageBox;
using WpfApplication = System.Windows.Application;

namespace QuickSub
{
    public partial class SettingsWindow : Window
    {
        public event EventHandler? SettingsChanged;

        public SettingsWindow()
        {
            InitializeComponent();
            LoadCurrentSettings();
            
            // Set window icon
            try
            {
                this.Icon = IconHelper.CreateAppIcon();
            }
            catch
            {
                // If icon creation fails, continue without icon
            }
            
            // Subscribe to settings changes to update UI
            QuickSubSettings.SettingsChanged += OnSettingsChanged;
        }

        private void LoadCurrentSettings()
        {
            var settings = QuickSubSettings.Instance;
            
            // Set current values
            SetComboBoxValue(FontSizeComboBox, settings.FontSize.ToString());
            SetComboBoxValue(ColorComboBox, settings.ColorIndex.ToString());
            SetComboBoxValue(BackgroundColorComboBox, settings.BackgroundColorIndex.ToString());
            SetComboBoxValue(OpacityComboBox, settings.Opacity.ToString());
            SetComboBoxValue(DisplayModeComboBox, settings.DisplayMode.ToString());
            
            SetComboBoxValue(TargetLanguageComboBox, settings.TargetLanguage ?? "ru");
        }

        private void SetComboBoxValue(WpfComboBox comboBox, string value)
        {
            foreach (WpfComboBoxItem item in comboBox.Items)
            {
                if (item.Tag?.ToString() == value)
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void DefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset all settings to defaults
            QuickSubSettings.Instance.ResetToDefaults();
            LoadCurrentSettings();
            
            WpfMessageBox.Show("Settings have been reset to defaults.", 
                          "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = QuickSubSettings.Instance;
                
                
                if (TargetLanguageComboBox.SelectedItem is WpfComboBoxItem langItem)
                    settings.TargetLanguage = langItem.Tag?.ToString() ?? "ru";
                
                if (FontSizeComboBox.SelectedItem is WpfComboBoxItem fontItem)
                    settings.FontSize = int.Parse(fontItem.Tag?.ToString() ?? "18");
                
                if (ColorComboBox.SelectedItem is WpfComboBoxItem colorItem)
                    settings.ColorIndex = int.Parse(colorItem.Tag?.ToString() ?? "1");
                
                if (BackgroundColorComboBox.SelectedItem is WpfComboBoxItem bgColorItem)
                    settings.BackgroundColorIndex = int.Parse(bgColorItem.Tag?.ToString() ?? "8");
                
                if (OpacityComboBox.SelectedItem is WpfComboBoxItem opacityItem)
                    settings.Opacity = byte.Parse(opacityItem.Tag?.ToString() ?? "150");
                
                if (DisplayModeComboBox.SelectedItem is WpfComboBoxItem displayModeItem)
                {
                    int displayMode = int.Parse(displayModeItem.Tag?.ToString() ?? "0");
                    settings.DisplayMode = displayMode;
                    
                    // Set ShowOriginal and ShowTranslation based on display mode
                    switch (displayMode)
                    {
                        case 0: // Both
                            settings.ShowOriginal = true;
                            settings.ShowTranslation = true;
                            break;
                        case 1: // Translation only
                            settings.ShowOriginal = false;
                            settings.ShowTranslation = true;
                            break;
                        case 2: // Original only
                            settings.ShowOriginal = true;
                            settings.ShowTranslation = false;
                            break;
                    }
                }
                
                // Save settings
                settings.Save();
                
                // Notify about changes
                SettingsChanged?.Invoke(this, EventArgs.Empty);
                
                WpfMessageBox.Show("Settings applied successfully!", 
                              "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Error applying settings: {ex.Message}", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        
        private void OnSettingsChanged(object? sender, EventArgs e)
        {
            // Update UI when settings change from overlay
            // Make sure we're on the UI thread
            if (Dispatcher.CheckAccess())
            {
                LoadCurrentSettings();
            }
            else
            {
                Dispatcher.Invoke(() => LoadCurrentSettings());
            }
        }
        
        private void ResetPositionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Find and reset overlay window position
                foreach (Window window in WpfApplication.Current.Windows)
                {
                    if (window is SubtitleOverlayWindow overlayWindow)
                    {
                        // Center window at bottom of screen
                        overlayWindow.Left = (SystemParameters.PrimaryScreenWidth - overlayWindow.Width) / 2;
                        overlayWindow.Top = SystemParameters.PrimaryScreenHeight - overlayWindow.Height - 100;
                        break;
                    }
                }
                
                WpfMessageBox.Show("Overlay window position has been reset!", 
                              "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Error resetting position: {ex.Message}", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        protected override void OnClosed(EventArgs e)
        {
            QuickSubSettings.SettingsChanged -= OnSettingsChanged;
            base.OnClosed(e);
        }
    }
}