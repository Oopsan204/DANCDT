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
        private static readonly Regex WordRegex = new Regex(@"([A-Z])\s*([+\-]?(?:\d+(?:\.\d*)?|\.\d+))", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

            var outputLines = new List<string>();
            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            bool convertNextCutterCompLeadInMove = false;

            for (int i = 0; i < lines.Length; i++)
            {
                bool isCutterCompLeadInLine = ContainsCutterCompWord(lines[i]);
                bool isCutterCompLeadOutLine = ContainsCutterCompOffWord(lines[i]);
                string cleaned = CleanLine(lines[i], i + 1, result);
                if (string.IsNullOrWhiteSpace(cleaned))
                    continue;

                foreach (string cleanedLine in cleaned.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(cleanedLine))
                        continue;

                    string normalizedLine = cleanedLine.Trim();

                    if (isCutterCompLeadOutLine)
                    {
                        ConvertCutterCompLeadOut(outputLines, normalizedLine, i + 1, result);
                        continue;
                    }

                    if (isCutterCompLeadInLine)
                    {
                        string rapidPositioning = ConvertToRapidPositioningLine(normalizedLine);
                        if (!string.IsNullOrWhiteSpace(rapidPositioning))
                            outputLines.Add(rapidPositioning);

                        convertNextCutterCompLeadInMove = true;
                        result.NormalizedLineCount++;
                        result.Warnings.Add("Line " + (i + 1) + ": converted cutter-comp lead-in move to rapid positioning.");
                        continue;
                    }

                    if (convertNextCutterCompLeadInMove && IsCutLine(SplitWords(normalizedLine)))
                    {
                        string rapidPositioning = ConvertToRapidPositioningLine(normalizedLine);
                        if (!string.IsNullOrWhiteSpace(rapidPositioning))
                            outputLines.Add(rapidPositioning);

                        convertNextCutterCompLeadInMove = false;
                        result.NormalizedLineCount++;
                        result.Warnings.Add("Line " + (i + 1) + ": converted cutter-comp lead-in arc/line to rapid positioning.");
                        continue;
                    }

                    outputLines.Add(normalizedLine);
                }
            }

            MoveLaserOnFromRapidToCut(outputLines, result);

            var output = new StringBuilder();
            foreach (string outputLine in outputLines)
                output.AppendLine(outputLine);

            result.Text = output.ToString();
            return result;
        }

        private static void MoveLaserOnFromRapidToCut(List<string> lines, CleanResult result)
        {
            if (lines == null || lines.Count == 0)
                return;

            bool pendingLaserOn = false;

            for (int i = 0; i < lines.Count; i++)
            {
                List<string> words = SplitWords(lines[i]);
                if (words.Count == 0)
                    continue;

                if (IsRapidLine(words) && HasMCode(words, 3))
                {
                    words = words.Where(word => !IsMCode(word, 3)).ToList();
                    lines[i] = string.Join(" ", words);
                    pendingLaserOn = true;
                    result.NormalizedLineCount++;
                    result.Warnings.Add("Moved M3 from rapid G0 to the first cut move to avoid a lead-in burn line.");
                    continue;
                }

                if (pendingLaserOn && IsCutLine(words))
                {
                    if (!words.Any(IsAnyMCode))
                    {
                        words.Add("M3");
                        lines[i] = string.Join(" ", words);
                    }
                    pendingLaserOn = false;
                }
            }
        }

        private static List<string> SplitWords(string line)
        {
            return string.IsNullOrWhiteSpace(line)
                ? new List<string>()
                : line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        private static bool ContainsCutterCompWord(string rawLine)
        {
            string line = RemoveComments(rawLine);
            foreach (Match match in WordRegex.Matches(line))
            {
                string letter = match.Groups[1].Value.ToUpperInvariant();
                if (letter != "G")
                    continue;

                int code = ParseWholeCode(match.Groups[2].Value);
                if (code == 41 || code == 42)
                    return true;
            }

            return false;
        }

        private static bool ContainsCutterCompOffWord(string rawLine)
        {
            string line = RemoveComments(rawLine);
            foreach (Match match in WordRegex.Matches(line))
            {
                string letter = match.Groups[1].Value.ToUpperInvariant();
                if (letter != "G")
                    continue;

                int code = ParseWholeCode(match.Groups[2].Value);
                if (code == 40)
                    return true;
            }

            return false;
        }

        private static void ConvertCutterCompLeadOut(List<string> outputLines, string currentLine, int sourceLineNumber, CleanResult result)
        {
            var leadOutIndices = new List<int>();
            for (int i = outputLines.Count - 1; i >= 0 && leadOutIndices.Count < 2; i--)
            {
                if (IsCutLine(SplitWords(outputLines[i])))
                    leadOutIndices.Add(i);
                else
                    break;
            }

            leadOutIndices.Reverse();
            int firstLeadOutIndex = leadOutIndices.Count > 0 ? leadOutIndices[0] : outputLines.Count;
            AttachLaserOffBeforeLeadOut(outputLines, firstLeadOutIndex);

            foreach (int index in leadOutIndices)
            {
                string rapidPositioning = ConvertToRapidPositioningLine(outputLines[index]);
                outputLines[index] = rapidPositioning;
            }

            for (int i = outputLines.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrWhiteSpace(outputLines[i]))
                    outputLines.RemoveAt(i);
            }

            string currentRapid = ConvertToRapidPositioningLine(currentLine);
            if (!string.IsNullOrWhiteSpace(currentRapid))
                outputLines.Add(currentRapid);

            result.NormalizedLineCount++;
            result.Warnings.Add("Line " + sourceLineNumber + ": converted cutter-comp lead-out moves to rapid positioning.");
        }

        private static void AttachLaserOffBeforeLeadOut(List<string> outputLines, int firstLeadOutIndex)
        {
            for (int i = Math.Min(firstLeadOutIndex - 1, outputLines.Count - 1); i >= 0; i--)
            {
                List<string> words = SplitWords(outputLines[i]);
                if (!IsCutLine(words))
                    continue;

                if (!HasMCode(words, 4))
                {
                    words = words.Where(word => !IsMCode(word, 3)).ToList();
                    words.Add("M4");
                    outputLines[i] = string.Join(" ", words);
                }
                return;
            }

            outputLines.Add("M4");
        }

        private static string ConvertToRapidPositioningLine(string line)
        {
            List<string> words = SplitWords(line);
            var positionWords = words.Where(IsXyWord).ToList();
            if (positionWords.Count == 0)
                return string.Empty;

            return "G0 " + string.Join(" ", positionWords);
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
            bool hasZWord = false;

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
                else if (letter == "Z")
                {
                    hasZWord = true;
                    removed.Add(letter + valueText);
                }
                else if (letter == "X" || letter == "Y" || letter == "I" || letter == "J" || letter == "R" || letter == "F" || letter == "P")
                {
                    kept.Add(letter + valueText);
                }
                else
                {
                    removed.Add(letter + valueText);
                }
            }

            if (removed.Any(IsMachineHomeWord))
            {
                DropLine(result, lineNumber, rawLine, "machine home/setup line");
                return string.Empty;
            }

            bool hasXyOrArcData = kept.Any(IsXyOrArcWord);
            if (hasZWord && !hasXyOrArcData)
            {
                kept = kept.Where(word => !IsFeedOrDwellWord(word)).ToList();
                if (!kept.Any(IsMotionGWord))
                    kept.Clear();
            }

            if (kept.Count == 0)
            {
                DropLine(result, lineNumber, rawLine, removed.Count == 0 ? "unsupported line" : "only unsupported words: " + string.Join(" ", removed));
                return string.Empty;
            }

            if (removed.Any(IsUnsupportedSetupWord) && !kept.Any(IsXyOrArcWord))
            {
                DropLine(result, lineNumber, rawLine, "unsupported setup line");
                return string.Empty;
            }

            if (removed.Count > 0)
            {
                result.Warnings.Add("Line " + lineNumber + ": removed unsupported words: " + string.Join(" ", removed));
            }

            return BuildOutputLine(kept);
        }

        private static string BuildOutputLine(List<string> kept)
        {
            if (kept == null || kept.Count == 0)
                return string.Empty;

            bool hasMotion = kept.Any(IsMotionGWord);
            bool hasAxis = kept.Any(IsAxisOrFeedWord);
            var setupGCodes = kept.Where(IsSetupGWord).ToList();

            if (hasMotion && hasAxis && setupGCodes.Count > 0)
            {
                var outputLines = new List<string>();
                outputLines.AddRange(setupGCodes);

                var motionWords = kept.Where(word => !IsSetupGWord(word)).ToList();
                if (motionWords.Count > 0)
                    outputLines.Add(string.Join(" ", motionWords));

                return string.Join(Environment.NewLine, outputLines);
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
            return letter == 'X' || letter == 'Y' || letter == 'I' || letter == 'J' || letter == 'R' || letter == 'F' || letter == 'P';
        }

        private static bool IsXyOrArcWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return false;

            char letter = char.ToUpperInvariant(word[0]);
            return letter == 'X' || letter == 'Y' || letter == 'I' || letter == 'J' || letter == 'R';
        }

        private static bool IsXyWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return false;

            char letter = char.ToUpperInvariant(word[0]);
            return letter == 'X' || letter == 'Y';
        }

        private static bool IsFeedOrDwellWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return false;

            char letter = char.ToUpperInvariant(word[0]);
            return letter == 'F' || letter == 'P';
        }

        private static bool IsMachineHomeWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return false;

            string upper = word.ToUpperInvariant();
            return upper.StartsWith("G28");
        }

        private static bool IsMotionGWord(string word)
        {
            int code;
            return TryGetGCode(word, out code) && code >= 0 && code <= 3;
        }

        private static bool IsRapidLine(List<string> words)
        {
            return words != null && words.Any(IsRapidGWord);
        }

        private static bool IsCutLine(List<string> words)
        {
            return words != null
                && words.Any(IsCutMotionGWord)
                && words.Any(IsXyOrArcWord);
        }

        private static bool IsRapidGWord(string word)
        {
            int code;
            return TryGetGCode(word, out code) && code == 0;
        }

        private static bool IsCutMotionGWord(string word)
        {
            int code;
            return TryGetGCode(word, out code) && code >= 1 && code <= 3;
        }

        private static bool HasMCode(List<string> words, int expectedCode)
        {
            return words != null && words.Any(word => IsMCode(word, expectedCode));
        }

        private static bool IsMCode(string word, int expectedCode)
        {
            int code;
            return TryGetMCode(word, out code) && code == expectedCode;
        }

        private static bool IsAnyMCode(string word)
        {
            int code;
            return TryGetMCode(word, out code);
        }

        private static bool TryGetMCode(string word, out int code)
        {
            code = int.MinValue;
            if (string.IsNullOrWhiteSpace(word) || char.ToUpperInvariant(word[0]) != 'M' || word.Length < 2)
                return false;

            code = ParseWholeCode(word.Substring(1));
            return code != int.MinValue;
        }

        private static bool IsSetupGWord(string word)
        {
            int code;
            if (!TryGetGCode(word, out code))
                return false;

            return code == 20
                || code == 21
                || code == 90
                || code == 91
                || code == 92
                || (code >= 54 && code <= 59);
        }

        private static bool TryGetGCode(string word, out int code)
        {
            code = int.MinValue;
            if (string.IsNullOrWhiteSpace(word) || char.ToUpperInvariant(word[0]) != 'G' || word.Length < 2)
                return false;

            code = ParseWholeCode(word.Substring(1));
            return code != int.MinValue;
        }

        private static string FormatCodeValue(string valueText)
        {
            int code = ParseWholeCode(valueText);
            return code == int.MinValue ? valueText : code.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
