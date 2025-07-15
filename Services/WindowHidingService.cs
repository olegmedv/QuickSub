using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace QuickSub
{
    public static class ScreenPrivacyService
    {
        // Windows API constants
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_LAYERED = 0x00080000;
        
        // Modern screen capture constants (Windows 10 2004+)
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;
        private const int DWMWA_EXCLUDED_FROM_PEEK = 12;
        private const int DWMWA_CLOAK = 13;

        // Windows API imports
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern uint SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;

        /// <summary>
        /// Hides window from screen capture using Windows API methods
        /// </summary>
        public static void HideFromScreenCapture(Window window)
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) 
                {
                    // If handle not created yet, subscribe to SourceInitialized event
                    window.SourceInitialized += (s, e) => HideFromScreenCapture(window);
                    return;
                }

                // DWM attributes for additional protection
                int excludeFromPeek = 1;
                DwmSetWindowAttribute(hwnd, DWMWA_EXCLUDED_FROM_PEEK, ref excludeFromPeek, sizeof(int));

                // Basic window styles - only toolwindow, without layered
                var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                exStyle |= WS_EX_TOOLWINDOW; // Only toolwindow, no transparency
                SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);

                // Topmost for proper display
                SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
            catch (Exception)
            {
                // Silently ignore errors
            }
        }

        /// <summary>
        /// Enables advanced hiding - hides from screen capture but keeps visible to user
        /// </summary>
        public static void EnableAdvancedHiding(Window window)
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) return;

                // Only SetWindowDisplayAffinity without additional effects that could hide the window
                SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);

                // Do not apply transparency or cloaking - they can hide the window from user
            }
            catch (Exception)
            {
                // Silently ignore errors
            }
        }

        /// <summary>
        /// Shows window in screen capture (reverses hiding)
        /// </summary>
        public static void ShowInScreenCapture(Window window)
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) return;

                // Remove exclusion from screen capture
                SetWindowDisplayAffinity(hwnd, 0);

                // Re-enable in Windows Peek
                int excludeFromPeek = 0;
                DwmSetWindowAttribute(hwnd, DWMWA_EXCLUDED_FROM_PEEK, ref excludeFromPeek, sizeof(int));

                // Remove DWM cloaking
                int cloak = 0;
                DwmSetWindowAttribute(hwnd, DWMWA_CLOAK, ref cloak, sizeof(int));
            }
            catch (Exception)
            {
                // Silently ignore errors
            }
        }


        /// <summary>
        /// Applies all hiding methods for maximum protection
        /// </summary>
        public static void MaximizePrivacyProtection(Window window)
        {
            HideFromScreenCapture(window);
            EnableAdvancedHiding(window);
        }
    }
}