using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace QuickSub
{
    public class SubtitleModel : INotifyPropertyChanged
    {
        private static SubtitleModel? instance = null;
        public event PropertyChangedEventHandler? PropertyChanged;

        private string currentCaption = "";
        private string translatedCaption = "";
        private string overlayCaption = "🎤 Waiting for subtitles...";
        private DateTime lastUpdateTime = DateTime.Now;
        private bool showOriginal = true;
        private bool showTranslation = true;
        
        // Show only current sentence without history

        public string CurrentCaption
        {
            get => currentCaption;
            set
            {
                // Update only if text actually changed
                if (string.CompareOrdinal(currentCaption, value) != 0 && !string.IsNullOrWhiteSpace(value))
                {
                    currentCaption = value;
                    lastUpdateTime = DateTime.Now;
                    
                    // Truncate long text for overlay display
                    string displayText = SubtitleTextHelper.TruncateByByteSize(value, SubtitleTextHelper.MAXIMUM_LENGTH);
                    
                    // Process line breaks for optimal display
                    displayText = SubtitleTextHelper.ProcessLineBreaks(displayText, SubtitleTextHelper.COMPACT_LENGTH);
                    
                    currentCaption = displayText;
                    
                    // Translate text asynchronously
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            string translation = await SubtitleTranslator.Translate(displayText);
                            TranslatedCaption = translation;
                        }
                        catch
                        {
                            TranslatedCaption = "";
                        }
                    });
                    
                    UpdateOverlayCaption();
                    
                    OnPropertyChanged();
                }
            }
        }

        public string TranslatedCaption
        {
            get => translatedCaption;
            set
            {
                if (translatedCaption != value)
                {
                    translatedCaption = value;
                    OnPropertyChanged();
                    UpdateOverlayCaption();
                }
            }
        }

        public string OverlayCaption
        {
            get => overlayCaption;
            set
            {
                if (overlayCaption != value)
                {
                    overlayCaption = value;
                    OnPropertyChanged();
                }
            }
        }

        private void UpdateOverlayCaption()
        {
            // Update overlay caption for display
            if (!string.IsNullOrWhiteSpace(currentCaption))
            {
                OverlayCaption = currentCaption;
            }
        }

        public DateTime LastUpdateTime
        {
            get => lastUpdateTime;
            set
            {
                if (lastUpdateTime != value)
                {
                    lastUpdateTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowOriginal
        {
            get => showOriginal;
            set
            {
                if (showOriginal != value)
                {
                    showOriginal = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowTranslation
        {
            get => showTranslation;
            set
            {
                if (showTranslation != value)
                {
                    showTranslation = value;
                    OnPropertyChanged();
                }
            }
        }

        private SubtitleModel()
        {
            LoadSettings();
        }

        public static SubtitleModel GetInstance()
        {
            if (instance != null)
                return instance;
            instance = new SubtitleModel();
            return instance;
        }

        // Show only current sentence without history

        public void Clear()
        {
            currentCaption = "";
            translatedCaption = "";
            OverlayCaption = "🎤 Waiting for subtitles...";
        }

        public void SetStatusMessage(string message)
        {
            OverlayCaption = message;
        }

        public void LoadSettings()
        {
            var settings = QuickSubSettings.Instance;
            ShowOriginal = settings.ShowOriginal;
            ShowTranslation = settings.ShowTranslation;
        }

        public void OnPropertyChanged([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
} 