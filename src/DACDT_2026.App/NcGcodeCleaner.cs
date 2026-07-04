using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DACDT_2026
{
    public static class NcGcodeCleaner
    {
        private static readonly Regex LineNumberRegex = new Regex(@"^\s*N\d+\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex WordRegex = new Regex(@"([A-Z])\s*([+\-]?\d+(?:\.\d*)?)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly HashSet<int> AllowedGCodes = new HashSet<int> { 0, 1, 2, 3, 4, 20, 21, 54, 55, 56, 57, 58, 59, 90, 91, 92 };
        private static readonly HashSet<int> DroppedGCodes = new HashSet<int> { 17, 18, 19, 28, 40, 41, 42, 43, 49, 53, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 94, 95, 98, 99 };
        private static readonly HashSet<int> AllowedMCodes = new HashSet<int> { 0, 1, 2, 3, 4, 5, 30 };
        private static readonly HashSet<int> DroppedMCodes = new HashSet<int> { 6, 7, 8, 9 };

        public sealed class CleanResult
        {
            public string Text { get; set; }
            public int RemovedLineCount { get; set; }
            public int NormalizedLineCount { get; set; }
            public List<string> Warnings { get; } = new List<string>();
        }

        public static CleanResult Clean(string text)
        {
            var result = new CleanResult();
            if (string.IsNullOrWhiteSpace(text))
            {
                result.Text = string.Empty;
                return result;
            }

            var output = new StringBuilder();
            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string cleaned = CleanLine(lines[i], i + 1, result);
                if (string.IsNullOrWhiteSpace(cleaned))
                    continue;

                output.AppendLine(cleaned);
            }

            result.Text = output.ToString();
            return result;
        }

        private static string CleanLine(string rawLine, int lineNumber, CleanResult result)
        {
            string line = RemoveComments(rawLine).Trim();
            if (string.IsNullOrWhiteSpace(line))
                return string.Empty;

            if (line == "%")
            {
                DropLine(result, lineNumber, rawLine, "program delimiter");
                return string.Empty;
            }

            line = LineNumberRegex.Replace(line, string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                DropLine(result, lineNumber, rawLine, "line number only");
                return string.Empty;
            }

            if (Regex.IsMatch(line, @"^O\d+", RegexOptions.IgnoreCase))
            {
                DropLine(result, lineNumber, rawLine, "program number");
                return string.Empty;
            }

            List<string> kept = new List<string>();
            List<string> removed = new List<string>();

            foreach (Match match in WordRegex.Matches(line))
            {
                string letter = match.Groups[1].Value.ToUpperInvariant();
                string valueText = match.Groups[2].Value;
                int codeValue;

                if (letter == "G")
                {
                    codeValue = ParseWholeCode(valueText);
                    if (AllowedGCodes.Contains(codeValue))
                        kept.Add("G" + FormatCodeValue(valueText));
                    else
                    {
                        removed.Add("G" + FormatCodeValue(valueText));
                    }
                }
                else if (letter == "M")
                {
                    codeValue = ParseWholeCode(valueText);
                    if (codeValue == 5)
                    {
                        kept.Add("M4");
                        result.NormalizedLineCount++;
                        result.Warnings.Add("Line " + lineNumber + ": normalized M5 to M4 for laser-off.");
                    }
                    else if (AllowedMCodes.Contains(codeValue))
                        kept.Add("M" + FormatCodeValue(valueText));
                    else if (DroppedMCodes.Contains(codeValue))
                        removed.Add("M" + FormatCodeValue(valueText));
                    else
                        removed.Add("M" + FormatCodeValue(valueText));
                }
                else if (letter == "X" || letter == "Y" || letter == "Z" || letter == "I" || letter == "J" || letter == "R" || letter == "F" || letter == "P")
                {
                    kept.Add(letter + valueText);
                }
                else
                {
                    removed.Add(letter + valueText);
                }
            }

            if (kept.Count == 0)
            {
                DropLine(result, lineNumber, rawLine, removed.Count == 0 ? "unsupported line" : "only unsupported words: " + string.Join(" ", removed));
                return string.Empty;
            }

            if (removed.Any(IsUnsupportedSetupWord) && kept.All(IsAxisOrFeedWord))
            {
                DropLine(result, lineNumber, rawLine, "unsupported setup line");
                return string.Empty;
            }

            if (removed.Count > 0)
            {
                result.Warnings.Add("Line " + lineNumber + ": removed unsupported words: " + string.Join(" ", removed));
            }

            return string.Join(" ", kept);
        }

        private static string RemoveComments(string rawLine)
        {
            string line = rawLine ?? string.Empty;
            int semicolon = line.IndexOf(';');
            if (semicolon >= 0)
                line = line.Substring(0, semicolon);

            return Regex.Replace(line, @"\([^)]*\)", string.Empty).Trim();
        }

        private static void DropLine(CleanResult result, int lineNumber, string rawLine, string reason)
        {
            result.RemovedLineCount++;
            result.Warnings.Add("Line " + lineNumber + ": removed " + reason + " [" + (rawLine ?? string.Empty).Trim() + "]");
        }

        private static int ParseWholeCode(string valueText)
        {
            double value;
            if (!double.TryParse(valueText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value))
                return int.MinValue;

            return (int)Math.Round(value);
        }

        private static bool IsUnsupportedSetupWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return false;

            string upper = word.ToUpperInvariant();
            return upper.StartsWith("G28")
                || upper.StartsWith("G40")
                || upper.StartsWith("G41")
                || upper.StartsWith("G42")
                || upper.StartsWith("G43")
                || upper.StartsWith("G49")
                || upper.StartsWith("G80")
                || upper.StartsWith("G81")
                || upper.StartsWith("G82")
                || upper.StartsWith("G83")
                || upper.StartsWith("G84")
                || upper.StartsWith("G85")
                || upper.StartsWith("G86")
                || upper.StartsWith("G87")
                || upper.StartsWith("G88")
                || upper.StartsWith("G89")
                || upper.StartsWith("M6")
                || upper.StartsWith("M7")
                || upper.StartsWith("M8")
                || upper.StartsWith("M9")
                || upper.StartsWith("T")
                || upper.StartsWith("H")
                || upper.StartsWith("D");
        }

        private static bool IsAxisOrFeedWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return false;

            char letter = char.ToUpperInvariant(word[0]);
            return letter == 'X' || letter == 'Y' || letter == 'Z' || letter == 'I' || letter == 'J' || letter == 'R' || letter == 'F' || letter == 'P';
        }

        private static string FormatCodeValue(string valueText)
        {
            int code = ParseWholeCode(valueText);
            return code == int.MinValue ? valueText : code.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
