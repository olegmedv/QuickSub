using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Net.Http;

namespace QuickSub
{
    public static class SubtitleTranslator
    {
        private static readonly HttpClient _httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(20) };

        public static async Task<string> Translate(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            try
            {
                // Use Google Translate - same logic as LiveCaptions-Translator
                var googleResult = await TranslateWithGoogle(text);
                return googleResult;
            }
            catch (Exception ex)
            {
                // Return error message like LiveCaptions-Translator does
                System.Diagnostics.Debug.WriteLine($"Translation error: {ex.Message}");
                return text; // Return original text if translation fails
            }
        }

        // Public method to gracefully shut down when application closes
        public static async Task Shutdown()
        {
            _httpClient?.Dispose();
        }

        private static async Task<string> TranslateWithGoogle(string text)
        {
            try
            {
                string targetLanguage = QuickSubSettings.Instance.TargetLanguage ?? "ru";

                string encodedText = Uri.EscapeDataString(text);
                var url = $"https://clients5.google.com/translate_a/t?" +
                          $"client=dict-chrome-ex&sl=auto&" +
                          $"tl={targetLanguage}&" +
                          $"q={encodedText}";

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await _httpClient.GetAsync(url, cts.Token);
                
                if (response.IsSuccessStatusCode)
                {
                    string responseString = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonSerializer.Deserialize<List<List<string>>>(responseString);
                    
                    if (responseObj != null && responseObj.Count > 0 && responseObj[0].Count > 0)
                    {
                        string translatedText = responseObj[0][0];
                        if (!string.IsNullOrWhiteSpace(translatedText))
                        {
                            System.Diagnostics.Debug.WriteLine($"Google Translate: '{text}' -> '{translatedText}'");
                            return translatedText;
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Google Translate API error: HTTP {response.StatusCode}");
                }
            }
            catch (TaskCanceledException ex)
            {
                if (ex.InnerException is TimeoutException)
                {
                    System.Diagnostics.Debug.WriteLine("Google Translate timeout (> 5 seconds)");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Google Translate request canceled: {ex.Message}");
                }
            }
            catch (OperationCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Google Translate operation canceled: {ex.Message}");
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Google Translate HTTP error: {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Google Translate unexpected error: {ex.Message}");
            }
            
            // Return original text if translation fails (like LiveCaptions-Translator)
            return text;
        }
    }
}