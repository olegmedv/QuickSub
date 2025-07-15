using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfApplication = System.Windows.Application;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;

namespace QuickSub
{
    public partial class SubtitleOverlayWindow : Window
    {
        private readonly Dictionary<int, WpfBrush> SubtitleColors = new Dictionary<int, WpfBrush> 
        {
            {1, WpfBrushes.White},
            {2, WpfBrushes.Orange},
            {3, WpfBrushes.LightGreen},
            {4, WpfBrushes.SkyBlue},
            {5, WpfBrushes.RoyalBlue},
            {6, WpfBrushes.Magenta},
            {7, WpfBrushes.Crimson},
            {8, WpfBrushes.DarkGray},
            {9, WpfBrushes.Gold},
            {10, WpfBrushes.Violet}
        };

        private DispatcherTimer? fadeTimer;
        private DispatcherTimer? controlPanelHideTimer;
        private DateTime lastUpdateTime;

        public SubtitleModel Caption { get; private set; }

        public SubtitleOverlayWindow()
        {
            InitializeComponent();
            
            // Initialize model and set DataContext
            Caption = SubtitleModel.GetInstance();
            DataContext = Caption;
            
            // Ensure settings are applied after full window initialization
            this.Loaded += (s, e) => RefreshDisplaySettings();
            
            SetupWindow();
            SetupFadeTimer();
            SetupControlPanelTimer();
            
            Caption.PropertyChanged += Caption_PropertyChanged;
            QuickSubSettings.SettingsChanged += OnSettingsChanged;
            RefreshDisplaySettings();
            UpdateVisibility();
        }

        private void Caption_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SubtitleModel.CurrentCaption))
            {
                // Reset opacity and restart timer on new subtitle
                lastUpdateTime = DateTime.Now;
                this.Opacity = 1.0;
                fadeTimer?.Stop();
                fadeTimer?.Start();
            }
            else if (e.PropertyName == nameof(SubtitleModel.ShowOriginal) || e.PropertyName == nameof(SubtitleModel.ShowTranslation))
            {
                // Update appearance when visibility changes
                UpdateVisibility();
            }
        }

        private void SetupWindow()
        {
            // Setup window properties and hide from screen capture
            this.SourceInitialized += (s, e) =>
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                
                // Hide window from screen sharing applications
                ScreenPrivacyService.HideFromScreenCapture(this);
                ScreenPrivacyService.EnableAdvancedHiding(this);
            };

            // Position window
            this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;
            this.Top = SystemParameters.PrimaryScreenHeight - this.Height - 100;

            // Set control panel visibility
            ControlPanel.Visibility = Visibility.Hidden;
        }

        private void SetupFadeTimer()
        {
            fadeTimer = new DispatcherTimer();
            fadeTimer.Interval = TimeSpan.FromSeconds(1);
            fadeTimer.Tick += FadeTimer_Tick;
        }

        private void SetupControlPanelTimer()
        {
            controlPanelHideTimer = new DispatcherTimer();
            controlPanelHideTimer.Interval = TimeSpan.FromMilliseconds(300); // 300ms delay
            controlPanelHideTimer.Tick += ControlPanelHideTimer_Tick;
        }

        private void ControlPanelHideTimer_Tick(object? sender, EventArgs e)
        {
            controlPanelHideTimer?.Stop();
            // Check if mouse is still over window or control panel
            if (!this.IsMouseOver)
            {
                ControlPanel.Visibility = Visibility.Hidden;
            }
        }

        private void FadeTimer_Tick(object? sender, EventArgs e)
        {
            var timeSinceLastUpdate = DateTime.Now - lastUpdateTime;
            var normalizedOpacity = Math.Min(255, Math.Max(1, (int)QuickSubSettings.Instance.Opacity)) / 255.0;
            
            if (timeSinceLastUpdate.TotalSeconds > 10) // Start fading after 10 seconds
            {
                var fadeSeconds = timeSinceLastUpdate.TotalSeconds - 10;
                var newOpacity = Math.Max(0.3, normalizedOpacity - (fadeSeconds / 20.0));
                this.Opacity = newOpacity;
            }
            else
            {
                this.Opacity = normalizedOpacity;
            }
        }

        // Update subtitles through the model
        public void UpdateSubtitles(string newSubtitle)
        {
            Caption.CurrentCaption = newSubtitle;
        }

        public void ClearSubtitles()
        {
            Caption.Clear();
            fadeTimer?.Stop();
            var normalizedOpacity = Math.Min(255, Math.Max(1, (int)QuickSubSettings.Instance.Opacity)) / 255.0;
            this.Opacity = normalizedOpacity;
        }

        public void SetStatusMessage(string message)
        {
            Caption.SetStatusMessage(message);
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void Window_MouseEnter(object sender, WpfMouseEventArgs e)
        {
            // Show control panel when mouse enters window
            controlPanelHideTimer?.Stop();
            ControlPanel.Visibility = Visibility.Visible;
        }

        private void Window_MouseLeave(object sender, WpfMouseEventArgs e)
        {
            // Start timer to hide control panel after delay
            controlPanelHideTimer?.Start();
        }

        private void ControlPanel_MouseEnter(object sender, WpfMouseEventArgs e)
        {
            // Keep control panel visible when mouse enters it
            controlPanelHideTimer?.Stop();
            ControlPanel.Visibility = Visibility.Visible;
        }

        private void ControlPanel_MouseLeave(object sender, WpfMouseEventArgs e)
        {
            // Start timer to hide control panel after delay
            controlPanelHideTimer?.Start();
        }

        private void FontSizeIncrease_Click(object sender, RoutedEventArgs e)
        {
            var settings = QuickSubSettings.Instance;
            if (settings.FontSize < 48)
            {
                settings.FontSize += 1;
                settings.Save();
                RefreshDisplaySettings();
            }
        }

        private void FontSizeDecrease_Click(object sender, RoutedEventArgs e)
        {
            var settings = QuickSubSettings.Instance;
            if (settings.FontSize > 10)
            {
                settings.FontSize -= 1;
                settings.Save();
                RefreshDisplaySettings();
            }
        }

        private void TextColorCycle_Click(object sender, RoutedEventArgs e)
        {
            var settings = QuickSubSettings.Instance;
            settings.ColorIndex++;
            if (settings.ColorIndex > SubtitleColors.Count)
                settings.ColorIndex = 1;
            settings.Save();
            RefreshDisplaySettings();
        }

        private void OpacityIncrease_Click(object sender, RoutedEventArgs e)
        {
            var settings = QuickSubSettings.Instance;
            if (settings.Opacity + 20 < 251)
                settings.Opacity += 20;
            else
                settings.Opacity = 251;
            settings.Save();
            RefreshDisplaySettings();
        }

        private void OpacityDecrease_Click(object sender, RoutedEventArgs e)
        {
            var settings = QuickSubSettings.Instance;
            if (settings.Opacity - 20 > 1)
                settings.Opacity -= 20;
            else
                settings.Opacity = 1;
            settings.Save();
            RefreshDisplaySettings();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            WpfApplication.Current.Shutdown();
        }

        private void UpdateFontSize()
        {
            var fontSize = QuickSubSettings.Instance.FontSize;
            PrimaryTextBlock.FontSize = fontSize;
            SecondaryTextBlock.FontSize = fontSize + 3; // Translation slightly larger
        }

        private void UpdateColors()
        {
            PrimaryTextBlock.Foreground = SubtitleColors[QuickSubSettings.Instance.ColorIndex];
            SecondaryTextBlock.Foreground = SubtitleColors[QuickSubSettings.Instance.ColorIndex];
        }

        private void UpdateOpacity()
        {
            this.Opacity = 1.0; // Window opacity always 1.0, background opacity is handled separately
        }
        
        private void UpdateBackgroundOpacity()
        {
            System.Windows.Media.Color color = ((SolidColorBrush)BorderBackground.Background).Color;
            var opacity = Math.Min(255, Math.Max(1, (int)QuickSubSettings.Instance.Opacity)); // Limit opacity from 1 to 255
            BorderBackground.Background = new SolidColorBrush(
                System.Windows.Media.Color.FromArgb((byte)opacity, color.R, color.G, color.B));
        }

        public void RefreshDisplaySettings()
        {
            var settings = QuickSubSettings.Instance;
            
            System.Diagnostics.Debug.WriteLine($"RefreshDisplaySettings() called - FontSize: {settings.FontSize}, ColorIndex: {settings.ColorIndex}, BackgroundColorIndex: {settings.BackgroundColorIndex}, Opacity: {settings.Opacity}, DisplayMode: {settings.DisplayMode}");
            
            // Apply font size (uniform size distribution)
            PrimaryTextBlock.FontSize = settings.FontSize;
            SecondaryTextBlock.FontSize = settings.FontSize - 1; // Slightly smaller for translation

            // Apply text color
            PrimaryTextBlock.Foreground = SubtitleColors[settings.ColorIndex];
            SecondaryTextBlock.Foreground = SubtitleColors[settings.ColorIndex];

            // Apply background color with proper opacity limit
            var bgColor = ((SolidColorBrush)SubtitleColors[settings.BackgroundColorIndex]).Color;
            var opacity = Math.Min(255, Math.Max(50, (int)settings.Opacity)); // Minimum opacity for readability
            BorderBackground.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)opacity, bgColor.R, bgColor.G, bgColor.B));

            System.Diagnostics.Debug.WriteLine($"Applied background color: {bgColor}, opacity: {opacity}");

            // Apply display mode
            SetDisplayMode(settings.DisplayMode);

            // Apply original and translation visibility
            Caption.ShowOriginal = settings.ShowOriginal;
            Caption.ShowTranslation = settings.ShowTranslation;
            UpdateVisibility();
        }
        
        private void UpdateVisibility()
        {
            bool showOriginal = Caption.ShowOriginal;
            bool showTranslation = Caption.ShowTranslation;
            
            // Update visibility based on current display mode
            PrimaryTextBorder.Visibility = showOriginal ? Visibility.Visible : Visibility.Collapsed;
            SecondaryTextBorder.Visibility = showTranslation ? Visibility.Visible : Visibility.Collapsed;
            
            // Find the divider line and update its visibility
            var parentGrid = PrimaryTextBorder.Parent as Grid;
            if (parentGrid != null)
            {
                foreach (var child in parentGrid.Children)
                {
                    if (child is System.Windows.Shapes.Rectangle divider)
                    {
                        divider.Visibility = (showOriginal && showTranslation) ? Visibility.Visible : Visibility.Collapsed;
                        break;
                    }
                }
            }
        }

        private void BackgroundColorCycle_Click(object sender, RoutedEventArgs e)
        {
            var settings = QuickSubSettings.Instance;
            settings.BackgroundColorIndex++;
            if (settings.BackgroundColorIndex > SubtitleColors.Count)
                settings.BackgroundColorIndex = 1;
            settings.Save();
            RefreshDisplaySettings();
        }

        private void OnlyModeButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = QuickSubSettings.Instance;
            if (settings.DisplayMode == 2)
            {
                settings.DisplayMode = 0;
                settings.ShowOriginal = true;
                settings.ShowTranslation = true;
            }
            else if (settings.DisplayMode == 0)
            {
                settings.DisplayMode = 1;
                settings.ShowOriginal = false;
                settings.ShowTranslation = true;
            }
            else
            {
                settings.DisplayMode = 2;
                settings.ShowOriginal = true;
                settings.ShowTranslation = false;
            }
            settings.Save();
            RefreshDisplaySettings();
        }

        private void SetDisplayMode(int displayMode)
        {
            switch (displayMode)
            {
                case 0: // Both
                    Caption.ShowOriginal = true;
                    Caption.ShowTranslation = true;
                    break;
                case 1: // Translation only
                    Caption.ShowOriginal = false;
                    Caption.ShowTranslation = true;
                    break;
                case 2: // Original only
                    Caption.ShowOriginal = true;
                    Caption.ShowTranslation = false;
                    break;
            }
            
            // Do NOT update settings here - this is for loading only
        }
        
        
        
        
        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
        }
        
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
        }
        
        private void OnSettingsChanged(object? sender, EventArgs e)
        {
            // Apply settings when they change from settings window
            // Make sure we're on the UI thread
            if (Dispatcher.CheckAccess())
            {
                RefreshDisplaySettings();
            }
            else
            {
                Dispatcher.Invoke(() => RefreshDisplaySettings());
            }
        }


        protected override void OnClosed(EventArgs e)
        {
            fadeTimer?.Stop();
            controlPanelHideTimer?.Stop();
            if (Caption != null)
            {
                Caption.PropertyChanged -= Caption_PropertyChanged;
            }
            QuickSubSettings.SettingsChanged -= OnSettingsChanged;
            base.OnClosed(e);
        }
    }
} 