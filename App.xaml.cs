using System.Windows;
using System;
using WinForms = System.Windows.Forms;
using WpfApplication = System.Windows.Application;

namespace QuickSub
{
    public partial class App : WpfApplication
    {
        private SubtitleTray? trayIcon;
        private SettingsWindow? settingsWindow;
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Explicit settings initialization (creates settings.json if it doesn't exist)
            var _ = QuickSubSettings.Instance;
            
            // Set application shutdown mode
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            
            // Initialize system tray icon
            InitializeTrayIcon();
        }
        
        private void InitializeTrayIcon()
        {
            trayIcon = new SubtitleTray();
            trayIcon.ShowSettingsRequested += TrayIcon_ShowSettingsRequested;
            trayIcon.ExitRequested += TrayIcon_ExitRequested;
            
            // Show startup notification
            trayIcon.ShowBalloonTip("QuickSub", "Application started and running in background", WinForms.ToolTipIcon.Info);
        }
        
        private void TrayIcon_ShowSettingsRequested(object? sender, EventArgs e)
        {
            if (settingsWindow == null || !settingsWindow.IsVisible)
            {
                settingsWindow = new SettingsWindow();
                settingsWindow.SettingsChanged += SettingsWindow_SettingsChanged;
                settingsWindow.Show();
            }
            else
            {
                settingsWindow.Activate();
            }
        }
        
        private void SettingsWindow_SettingsChanged(object? sender, EventArgs e)
        {
            // Apply settings to overlay window
            foreach (Window window in this.Windows)
            {
                if (window is SubtitleOverlayWindow overlayWindow)
                {
                    overlayWindow.RefreshDisplaySettings();
                }
            }
            
            // Notify about settings application
            trayIcon?.ShowBalloonTip("QuickSub", "Settings applied", WinForms.ToolTipIcon.Info);
        }
        
        private async void TrayIcon_ExitRequested(object? sender, EventArgs e)
        {
            // Clean up translation resources
            await SubtitleTranslator.Shutdown();
            
            // Close application
            trayIcon?.Dispose();
            settingsWindow?.Close();
            this.Shutdown();
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            // Clean up translation resources (synchronous version for OnExit)
            try
            {
                SubtitleTranslator.Shutdown().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during shutdown: {ex.Message}");
            }
            
            trayIcon?.Dispose();
            base.OnExit(e);
        }
    }
} 