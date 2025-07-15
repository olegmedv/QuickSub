using System;
using System.Runtime.InteropServices;
using System.Windows.Automation;

namespace QuickSub
{
    public static class LiveCaptionsWindowManager
    {
        private const int WINDOW_EXTENDED_STYLE = -20;
        private const int HIDDEN_WINDOW_FLAG = 0x00000080;
        
        private const int MINIMIZE_WINDOW = 6;
        private const int RESTORE_WINDOW = 9;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        
        /// <summary>
        /// Hides LiveCaptions window
        /// </summary>
        public static void ConcealLiveCaptionsWindow(AutomationElement captionsWindow)
        {
            try
            {
                IntPtr windowHandle = new IntPtr((long)captionsWindow.Current.NativeWindowHandle);
                int currentExtendedStyle = GetWindowLong(windowHandle, WINDOW_EXTENDED_STYLE);
                
                ShowWindow(windowHandle, MINIMIZE_WINDOW);
                SetWindowLong(windowHandle, WINDOW_EXTENDED_STYLE, currentExtendedStyle | HIDDEN_WINDOW_FLAG);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to conceal LiveCaptions window: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Restores LiveCaptions window
        /// </summary>
        public static void RevealLiveCaptionsWindow(AutomationElement captionsWindow)
        {
            try
            {
                IntPtr windowHandle = new IntPtr((long)captionsWindow.Current.NativeWindowHandle);
                int currentExtendedStyle = GetWindowLong(windowHandle, WINDOW_EXTENDED_STYLE);
                
                SetWindowLong(windowHandle, WINDOW_EXTENDED_STYLE, currentExtendedStyle & ~HIDDEN_WINDOW_FLAG);
                ShowWindow(windowHandle, RESTORE_WINDOW);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to reveal LiveCaptions window: {ex.Message}");
            }
        }
    }
} 