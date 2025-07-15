using System;
using System.Linq;
using System.Text;

namespace QuickSub
{
    public static class SubtitleTextHelper
    {
        private static readonly char[] SENTENCE_ENDINGS = { '.', '!', '?', '。', '！', '？', '…' };
        private static readonly char[] PAUSE_MARKERS = { ',', ';', ':', '，', '；', '：', '、', '—', '–', '\n' };

        private const int BRIEF_LENGTH = 8;
        public const int COMPACT_LENGTH = 35;
        private const int EXTENDED_LENGTH = 150;
        public const int MAXIMUM_LENGTH = 200;

        public static string TruncateByByteSize(string input, int maxBytes)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var workingText = input;
            while (Encoding.UTF8.GetByteCount(workingText) > maxBytes)
            {
                var breakPoint = FindOptimalBreakPoint(workingText);
                if (breakPoint < 0 || breakPoint >= workingText.Length - 1)
                    break;
                workingText = workingText.Substring(breakPoint + 1).TrimStart();
            }
            return workingText;
        }

        private static int FindOptimalBreakPoint(string text)
        {
            var allBreakChars = SENTENCE_ENDINGS.Concat(PAUSE_MARKERS).ToArray();
            return text.IndexOfAny(allBreakChars);
        }

        public static string ProcessLineBreaks(string input, int lengthThreshold)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var segments = input.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var processedSegments = new string[segments.Length];

            for (int i = 0; i < segments.Length; i++)
            {
                processedSegments[i] = segments[i].Trim();
                
                if (i < segments.Length - 1 && processedSegments[i].Length > 0)
                {
                    char finalChar = processedSegments[i][^1];
                    bool isLongSegment = Encoding.UTF8.GetByteCount(processedSegments[i]) >= lengthThreshold;
                    
                    if (IsAsianCharacter(finalChar))
                    {
                        processedSegments[i] += isLongSegment ? "。" : "——";
                    }
                    else
                    {
                        processedSegments[i] += isLongSegment ? ". " : "—";
                    }
                }
            }
            return string.Join("", processedSegments);
        }

        public static bool IsAsianCharacter(char character)
        {
            return (character >= '\u4E00' && character <= '\u9FFF') ||
                   (character >= '\u3400' && character <= '\u4DBF') ||
                   (character >= '\u3040' && character <= '\u30FF') ||
                   (character >= '\uAC00' && character <= '\uD7AF');
        }

        public static string WrapTextToLines(string input, int maxLineLength = 75)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= maxLineLength)
                return input;

            var tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var outputBuilder = new StringBuilder();
            var lineBuilder = new StringBuilder();

            foreach (var token in tokens)
            {
                if (lineBuilder.Length + token.Length + 1 > maxLineLength)
                {
                    if (lineBuilder.Length > 0)
                    {
                        outputBuilder.AppendLine(lineBuilder.ToString().Trim());
                        lineBuilder.Clear();
                    }
                }

                if (lineBuilder.Length > 0)
                    lineBuilder.Append(' ');
                lineBuilder.Append(token);
            }

            if (lineBuilder.Length > 0)
                outputBuilder.Append(lineBuilder.ToString().Trim());

            return outputBuilder.ToString();
        }

        public static double CalculateTextSimilarity(string first, string second)
        {
            if (string.IsNullOrEmpty(first) && string.IsNullOrEmpty(second))
                return 1.0;
            
            if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second))
                return 0.0;

            if (first.Contains(second) || second.Contains(first))
                return 0.95;

            return JaroWinklerSimilarity(first, second);
        }

        public static double JaroWinklerSimilarity(string s1, string s2)
        {
            if (s1 == s2) return 1.0;
            
            int len1 = s1.Length;
            int len2 = s2.Length;
            
            if (len1 == 0 || len2 == 0) return 0.0;

            int matchWindow = Math.Max(len1, len2) / 2 - 1;
            if (matchWindow < 0) matchWindow = 0;

            bool[] s1Matches = new bool[len1];
            bool[] s2Matches = new bool[len2];

            int matches = 0;
            int transpositions = 0;

            for (int i = 0; i < len1; i++)
            {
                int start = Math.Max(0, i - matchWindow);
                int end = Math.Min(i + matchWindow + 1, len2);

                for (int j = start; j < end; j++)
                {
                    if (s2Matches[j] || s1[i] != s2[j]) continue;
                    s1Matches[i] = true;
                    s2Matches[j] = true;
                    matches++;
                    break;
                }
            }

            if (matches == 0) return 0.0;

            int k = 0;
            for (int i = 0; i < len1; i++)
            {
                if (!s1Matches[i]) continue;
                while (!s2Matches[k]) k++;
                if (s1[i] != s2[k]) transpositions++;
                k++;
            }

            double jaro = (matches / (double)len1 + 
                          matches / (double)len2 + 
                          (matches - transpositions / 2.0) / matches) / 3.0;

            if (jaro < 0.7) return jaro;

            int prefix = 0;
            int maxPrefix = Math.Min(4, Math.Min(len1, len2));
            for (int i = 0; i < maxPrefix; i++)
            {
                if (s1[i] == s2[i]) prefix++;
                else break;
            }

            return jaro + (0.1 * prefix * (1.0 - jaro));
        }
    }
} 