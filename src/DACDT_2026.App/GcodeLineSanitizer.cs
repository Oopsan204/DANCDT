using System;
using System.Text;

namespace DACDT_2026
{
    public static class GcodeLineSanitizer
    {
        public static string NormalizeForParser(string rawLine)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
                return string.Empty;

            string line = rawLine;

            int checksumIndex = line.IndexOf('*');
            if (checksumIndex >= 0)
                line = line.Substring(0, checksumIndex);

            int semicolonIndex = line.IndexOf(';');
            if (semicolonIndex >= 0)
                line = line.Substring(0, semicolonIndex);

            line = StripParentheses(line).Trim();

            return HasIncompleteNumber(line) ? string.Empty : line;
        }

        private static bool HasIncompleteNumber(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            string[] tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string token in tokens)
            {
                if (token.Length < 2 || !char.IsLetter(token[0]))
                    continue;

                string numberText = token.Substring(1);
                if (string.IsNullOrEmpty(numberText))
                    return true;

                if (numberText == "-"
                    || numberText == "+"
                    || numberText == "."
                    || numberText == "-."
                    || numberText == "+.")
                    return true;

                if (numberText.EndsWith("-", StringComparison.Ordinal)
                    || numberText.EndsWith("+", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static string StripParentheses(string value)
        {
            var builder = new StringBuilder(value.Length);
            int depth = 0;

            foreach (char ch in value)
            {
                if (ch == '(')
                {
                    depth++;
                    continue;
                }

                if (ch == ')' && depth > 0)
                {
                    depth--;
                    continue;
                }

                if (depth == 0)
                    builder.Append(ch);
            }

            return builder.ToString();
        }
    }
}
