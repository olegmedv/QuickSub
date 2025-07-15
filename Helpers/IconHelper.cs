using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace QuickSub
{
    public static class IconHelper
    {
        public static BitmapSource CreateAppIcon()
        {
            // Create a 64x64 bitmap
            using (var bitmap = new Bitmap(64, 64))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);

                // Draw blue circle background
                using (var brush = new SolidBrush(Color.FromArgb(46, 134, 171)))
                {
                    graphics.FillEllipse(brush, 2, 2, 60, 60);
                }

                // Draw border
                using (var pen = new Pen(Color.FromArgb(35, 82, 124), 2))
                {
                    graphics.DrawEllipse(pen, 2, 2, 60, 60);
                }

                // Draw subtitle lines
                using (var whiteBrush = new SolidBrush(Color.White))
                {
                    // Top line
                    graphics.FillRectangle(whiteBrush, 8, 20, 48, 4);
                    // Second line
                    graphics.FillRectangle(whiteBrush, 12, 28, 40, 4);
                    // Third line
                    graphics.FillRectangle(whiteBrush, 8, 36, 48, 4);
                    // Bottom line
                    graphics.FillRectangle(whiteBrush, 16, 44, 32, 4);
                }

                // Draw "S" text
                using (var font = new Font("Arial", 10, System.Drawing.FontStyle.Bold))
                using (var whiteBrush = new SolidBrush(Color.White))
                {
                    var textSize = graphics.MeasureString("S", font);
                    var x = (64 - textSize.Width) / 2;
                    graphics.DrawString("S", font, whiteBrush, x, 12);
                }

                // Convert to BitmapSource
                var hBitmap = bitmap.GetHbitmap();
                try
                {
                    return Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }
                finally
                {
                    NativeMethods.DeleteObject(hBitmap);
                }
            }
        }

        public static Icon CreateSystemIcon()
        {
            // Create a 32x32 bitmap for system tray
            using (var bitmap = new Bitmap(32, 32))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);

                // Draw blue circle background
                using (var brush = new SolidBrush(Color.FromArgb(46, 134, 171)))
                {
                    graphics.FillEllipse(brush, 1, 1, 30, 30);
                }

                // Draw subtitle lines
                using (var whiteBrush = new SolidBrush(Color.White))
                {
                    graphics.FillRectangle(whiteBrush, 4, 10, 24, 2);
                    graphics.FillRectangle(whiteBrush, 6, 14, 20, 2);
                    graphics.FillRectangle(whiteBrush, 4, 18, 24, 2);
                    graphics.FillRectangle(whiteBrush, 8, 22, 16, 2);
                }

                // Draw "S" text
                using (var font = new Font("Arial", 6, System.Drawing.FontStyle.Bold))
                using (var whiteBrush = new SolidBrush(Color.White))
                {
                    var textSize = graphics.MeasureString("S", font);
                    var x = (32 - textSize.Width) / 2;
                    graphics.DrawString("S", font, whiteBrush, x, 6);
                }

                return Icon.FromHandle(bitmap.GetHicon());
            }
        }
        
        public static Bitmap CreateSettingsIcon()
        {
            var bitmap = new Bitmap(16, 16);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);

                // Draw gear icon
                using (var brush = new SolidBrush(Color.DimGray))
                {
                    // Outer circle
                    graphics.FillEllipse(brush, 2, 2, 12, 12);
                    // Inner circle (hole)
                    using (var whiteBrush = new SolidBrush(Color.White))
                    {
                        graphics.FillEllipse(whiteBrush, 5, 5, 6, 6);
                    }
                    // Gear teeth (simplified)
                    graphics.FillRectangle(brush, 7, 0, 2, 3);
                    graphics.FillRectangle(brush, 7, 13, 2, 3);
                    graphics.FillRectangle(brush, 0, 7, 3, 2);
                    graphics.FillRectangle(brush, 13, 7, 3, 2);
                }
            }
            return bitmap;
        }
        
        public static Bitmap CreateCloseIcon()
        {
            var bitmap = new Bitmap(16, 16);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);

                // Draw X icon
                using (var pen = new Pen(Color.DarkRed, 2))
                {
                    graphics.DrawLine(pen, 3, 3, 13, 13);
                    graphics.DrawLine(pen, 13, 3, 3, 13);
                }
            }
            return bitmap;
        }
    }

    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
    }
}