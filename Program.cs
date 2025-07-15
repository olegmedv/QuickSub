using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;

namespace QuickSub
{
    public class Program
    {
        private const string PROCESS_NAME = "LiveCaptions";
        private static AutomationElement? liveCaptionsWindow = null;
        private static AutomationElement? captionsTextBlock = null;
        private static string lastCaptionText = "";
        private static SubtitleOverlayWindow? overlayWindow = null;
        private static bool isCapturing = false;

        [STAThread]
        static void Main(string[] args)
        {
            // Check command line arguments
            bool consoleMode = args.Length > 0 && args[0] == "--console";
            
            if (consoleMode)
            {
                // Console mode (previous version)
                RunConsoleMode();
            }
            else
            {
                // Overlay mode (new version)
                RunOverlayMode();
            }
        }
        
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        private static void RunConsoleMode()
        {
            Console.WriteLine("=== QuickSub - Console Mode ===");
            Console.WriteLine("Press Ctrl+C to exit\n");

            try
            {
                Console.WriteLine("Starting Windows LiveCaptions...");
                LaunchLiveCaptions();

                if (liveCaptionsWindow == null)
                {
                    Console.WriteLine("❌ Failed to launch LiveCaptions!");
                    return;
                }

                Console.WriteLine("✅ LiveCaptions launched successfully!");
                Console.WriteLine("🎤 Starting caption capture...\n");

                isCapturing = true;
                RunCaptureLoop(null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Critical error: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        private static void RunOverlayMode()
        {
            
            var app = new App();
            
            // Create overlay window
            overlayWindow = new SubtitleOverlayWindow();
            app.MainWindow = overlayWindow;
            
            // Start caption capture in background thread
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(1000); // Give window time to initialize
                    
                    overlayWindow.Dispatcher.Invoke(() =>
                    {
                        overlayWindow.SetStatusMessage("🚀 Starting Windows LiveCaptions...");
                    });
                    
                    LaunchLiveCaptions();

                    if (liveCaptionsWindow == null)
                    {
                        overlayWindow.Dispatcher.Invoke(() =>
                        {
                            overlayWindow.SetStatusMessage("❌ Failed to launch LiveCaptions!");
                        });
                        return;
                    }

                    // Hide LiveCaptions window
                    LiveCaptionsWindowManager.ConcealLiveCaptionsWindow(liveCaptionsWindow);

                    overlayWindow.Dispatcher.Invoke(() =>
                    {
                        overlayWindow.SetStatusMessage("✅ LiveCaptions started! Speak into microphone...");
                    });

                    await Task.Delay(2000);
                    
                    overlayWindow.Dispatcher.Invoke(() =>
                    {
                        overlayWindow.ClearSubtitles();
                    });

                    isCapturing = true;
                    RunCaptureLoop(overlayWindow);
                }
                catch (Exception ex)
                {
                    overlayWindow?.Dispatcher.Invoke(() =>
                    {
                        overlayWindow.SetStatusMessage($"❌ Error: {ex.Message}");
                    });
                }
            });

            overlayWindow.Show();
            app.Run();
        }

        private static void LaunchLiveCaptions()
        {
            // Kill existing LiveCaptions processes
            TerminateAllLiveCaptionsInstances();

            // Launch new process
            var process = Process.Start(PROCESS_NAME);
            if (process == null)
            {
                throw new Exception("Failed to launch LiveCaptions process");
            }

            // Wait for window to appear
            AutomationElement? window = null;
            for (int attempts = 0; attempts < 100; attempts++)
            {
                window = FindWindowByProcessId(process.Id);
                if (window != null && window.Current.ClassName == "LiveCaptionsDesktopWindow")
                {
                    break;
                }
                Thread.Sleep(200);
            }

            if (window == null)
            {
                throw new Exception("Failed to find LiveCaptions window after launch");
            }

            liveCaptionsWindow = window;
        }

        private static AutomationElement? FindWindowByProcessId(int processId)
        {
            try
            {
                var condition = new PropertyCondition(AutomationElement.ProcessIdProperty, processId);
                return AutomationElement.RootElement.FindFirst(TreeScope.Children, condition);
            }
            catch
            {
                return null;
            }
        }

        private static string GetCurrentCaption()
        {
            if (liveCaptionsWindow == null)
                return "";
                
            if (captionsTextBlock == null)
            {
                captionsTextBlock = FindElementByAId(liveCaptionsWindow, "CaptionsTextBlock");
                if (captionsTextBlock == null)
                    return "";
            }

            try
            {
                // Get full text from LiveCaptions
                string fullText = captionsTextBlock.Current.Name ?? "";
                
                if (string.IsNullOrEmpty(fullText))
                    return "";

                // Process text (simplified version)
                fullText = CleanText(fullText);
                
                // Find last sentence
                char[] punctuationEOS = { '.', '!', '?', '。', '！', '？' };
                
                int lastEOSIndex;
                if (fullText.Length > 0 && Array.IndexOf(punctuationEOS, fullText[^1]) != -1)
                    lastEOSIndex = fullText[0..^1].LastIndexOfAny(punctuationEOS);
                else
                    lastEOSIndex = fullText.LastIndexOfAny(punctuationEOS);
                
                // Extract latest caption
                string latestCaption = fullText.Substring(lastEOSIndex + 1);
                
                // If sentence too short, add previous
                if (lastEOSIndex > 0 && latestCaption.Trim().Length < 10)
                {
                    lastEOSIndex = fullText[0..lastEOSIndex].LastIndexOfAny(punctuationEOS);
                    latestCaption = fullText.Substring(lastEOSIndex + 1);
                }
                
                // Return only last sentence
                return latestCaption.Trim();
            }
            catch (ElementNotAvailableException)
            {
                captionsTextBlock = null;
                throw;
            }
        }

        private static AutomationElement? FindElementByAId(AutomationElement parent, string automationId)
        {
            try
            {
                var condition = new PropertyCondition(AutomationElement.AutomationIdProperty, automationId);
                return parent.FindFirst(TreeScope.Descendants, condition);
            }
            catch
            {
                return null;
            }
        }

        private static string CleanText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Remove extra spaces and line breaks
            text = Regex.Replace(text, @"\s+", " ").Trim();
            
            // Process abbreviations (remove dots between capital letters)
            text = Regex.Replace(text, @"([A-Z])\s*\.\s*([A-Z])(?![A-Za-z]+)", "$1$2");
            
            // Remove extra spaces around punctuation
            text = Regex.Replace(text, @"\s*([.!?,])\s*", "$1 ");
            
            return text.Trim();
        }

        private static void RunCaptureLoop(SubtitleOverlayWindow? overlay)
        {
            while (isCapturing)
            {
                try
                {
                    string currentCaption = GetCurrentCaption();

                    // Update only if text actually changed
                    if (!string.IsNullOrEmpty(currentCaption) && 
                        string.CompareOrdinal(currentCaption, lastCaptionText) != 0)
                    {
                        lastCaptionText = currentCaption;

                        if (overlay != null)
                        {
                            // Send to overlay
                            overlay.Dispatcher.Invoke(() =>
                            {
                                overlay.UpdateSubtitles(currentCaption);
                            });
                        }
                        else
                        {
                            // Output to console
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {currentCaption}");
                        }
                        
                        // Silent operation - no console output
                    }
                }
                catch (ElementNotAvailableException)
                {
                    string errorMsg = "⚠️ Connection to LiveCaptions lost, reconnecting...";
                    
                    if (overlay != null)
                    {
                        overlay.Dispatcher.Invoke(() =>
                        {
                            overlay.SetStatusMessage(errorMsg);
                        });
                    }
                    else
                    {
                        Console.WriteLine(errorMsg);
                    }
                    
                    captionsTextBlock = null;
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    string errorMsg = $"❌ Error: {ex.Message}";
                    
                    if (overlay != null)
                    {
                        overlay.Dispatcher.Invoke(() =>
                        {
                            overlay.SetStatusMessage(errorMsg);
                        });
                    }
                    else
                    {
                        Console.WriteLine(errorMsg);
                    }
                }

                Thread.Sleep(25); // 25ms polling interval
            }
        }

        private static void TerminateAllLiveCaptionsInstances()
        {
            var runningProcesses = Process.GetProcessesByName(PROCESS_NAME);
            foreach (var processInstance in runningProcesses)
            {
                try
                {
                    processInstance.Kill();
                    processInstance.WaitForExit(5000); // 5 second timeout
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error terminating process: {ex.Message}");
                }
                finally
                {
                    processInstance?.Dispose();
                }
            }
        }
    }
} 