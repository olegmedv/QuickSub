using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using WinFormsApplication = System.Windows.Forms.Application;

namespace QuickSub
{
    public class SubtitleTray : IDisposable
    {
        private NotifyIcon? notifyIcon;
        private ContextMenuStrip? contextMenu;
        private ToolStripMenuItem? settingsMenuItem;
        private ToolStripMenuItem? exitMenuItem;

        public event EventHandler? ShowSettingsRequested;
        public event EventHandler? ExitRequested;

        public SubtitleTray()
        {
            InitializeSystemTray();
        }

        private void InitializeSystemTray()
        {
            // Create context menu
            contextMenu = new ContextMenuStrip();
            
            settingsMenuItem = new ToolStripMenuItem("Settings");
            settingsMenuItem.Click += SettingsMenuItem_Click;
            try
            {
                settingsMenuItem.Image = IconHelper.CreateSettingsIcon();
            }
            catch
            {
                // Continue without icon if creation fails
            }
            
            exitMenuItem = new ToolStripMenuItem("Exit");
            exitMenuItem.Click += ExitMenuItem_Click;
            try
            {
                exitMenuItem.Image = IconHelper.CreateCloseIcon();
            }
            catch
            {
                // Continue without icon if creation fails
            }
            
            contextMenu.Items.Add(settingsMenuItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitMenuItem);

            // Create tray icon
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = CreateIcon();
            notifyIcon.Text = "QuickSub - Live Subtitle Overlay";
            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.Visible = true;

            // Double click to open settings
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
        }

        private Icon CreateIcon()
        {
            try
            {
                return IconHelper.CreateSystemIcon();
            }
            catch
            {
                // Fallback to simple icon if helper fails
                using (var bitmap = new Bitmap(16, 16))
                {
                    using (var graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.Clear(Color.Transparent);
                        
                        // Draw simple icon - square with letter "S"
                        using (var brush = new SolidBrush(Color.DodgerBlue))
                        {
                            graphics.FillRectangle(brush, 2, 2, 12, 12);
                        }
                        
                        using (var font = new Font("Arial", 8, FontStyle.Bold))
                        using (var brush = new SolidBrush(Color.White))
                        {
                            graphics.DrawString("S", font, brush, 4, 2);
                        }
                    }
                    
                    return Icon.FromHandle(bitmap.GetHicon());
                }
            }
        }

        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            ShowSettingsRequested?.Invoke(this, EventArgs.Empty);
        }

        private void SettingsMenuItem_Click(object? sender, EventArgs e)
        {
            ShowSettingsRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExitMenuItem_Click(object? sender, EventArgs e)
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }

        public void ShowBalloonTip(string title, string text, System.Windows.Forms.ToolTipIcon icon = System.Windows.Forms.ToolTipIcon.Info)
        {
            notifyIcon?.ShowBalloonTip(3000, title, text, icon);
        }

        public void Dispose()
        {
            notifyIcon?.Dispose();
            contextMenu?.Dispose();
            settingsMenuItem?.Dispose();
            exitMenuItem?.Dispose();
        }
    }
}