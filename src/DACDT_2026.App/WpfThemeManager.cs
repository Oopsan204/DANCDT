using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace DACDT_2026
{
    public static class WpfThemeManager
    {
        public const string DarkTheme = "dark";
        public const string LightTheme = "light";

        private static readonly Dictionary<string, Color> DarkPalette = new Dictionary<string, Color>
        {
            ["BgBrush"] = Color.FromRgb(0x0B, 0x11, 0x20),
            ["PanelBrush"] = Color.FromRgb(0x11, 0x18, 0x27),
            ["PanelAltBrush"] = Color.FromRgb(0x16, 0x20, 0x33),
            ["BorderBrush"] = Color.FromRgb(0x26, 0x32, 0x47),
            ["BorderSoftBrush"] = Color.FromRgb(0x34, 0x42, 0x58),
            ["TextBrush"] = Color.FromRgb(0xE5, 0xE7, 0xEB),
            ["MutedBrush"] = Color.FromRgb(0x94, 0xA3, 0xB8),
            ["AccentBrush"] = Color.FromRgb(0x06, 0xB6, 0xD4),
            ["DataGridRowBrush"] = Color.FromRgb(0x10, 0x18, 0x27),
            ["DataGridAltRowBrush"] = Color.FromRgb(0x0D, 0x16, 0x26),
            ["DataGridHeaderBrush"] = Color.FromRgb(0x1F, 0x29, 0x37),
            ["HoverBrush"] = Color.FromRgb(0x22, 0x30, 0x49),
            ["SelectedBrush"] = Color.FromRgb(0x1D, 0x4B, 0x63),
            ["CardHeaderBrush"] = Color.FromRgb(0x18, 0x23, 0x3A),
            ["CardHeaderTextBrush"] = Color.FromRgb(0xE5, 0xE7, 0xEB),
            ["HelpCalloutBrush"] = Color.FromRgb(0x11, 0x18, 0x27),
            ["HelpCalloutBorderBrush"] = Color.FromRgb(0x33, 0x41, 0x55)
        };

        private static readonly Dictionary<string, Color> LightPalette = new Dictionary<string, Color>
        {
            ["BgBrush"] = Color.FromRgb(0xE9, 0xEE, 0xF5),
            ["PanelBrush"] = Color.FromRgb(0xFF, 0xFF, 0xFF),
            ["PanelAltBrush"] = Color.FromRgb(0xF4, 0xF7, 0xFB),
            ["BorderBrush"] = Color.FromRgb(0xB9, 0xC7, 0xD6),
            ["BorderSoftBrush"] = Color.FromRgb(0x7C, 0x8E, 0xA3),
            ["TextBrush"] = Color.FromRgb(0x10, 0x20, 0x33),
            ["MutedBrush"] = Color.FromRgb(0x5C, 0x6F, 0x86),
            ["AccentBrush"] = Color.FromRgb(0x0A, 0x7F, 0x95),
            ["DataGridRowBrush"] = Color.FromRgb(0xFF, 0xFF, 0xFF),
            ["DataGridAltRowBrush"] = Color.FromRgb(0xF7, 0xFA, 0xFD),
            ["DataGridHeaderBrush"] = Color.FromRgb(0xDB, 0xE6, 0xF3),
            ["HoverBrush"] = Color.FromRgb(0xD8, 0xED, 0xF5),
            ["SelectedBrush"] = Color.FromRgb(0xB9, 0xE3, 0xEF),
            ["CardHeaderBrush"] = Color.FromRgb(0xDC, 0xEA, 0xFE),
            ["CardHeaderTextBrush"] = Color.FromRgb(0x0C, 0x25, 0x40),
            ["HelpCalloutBrush"] = Color.FromRgb(0xF5, 0xF8, 0xFC),
            ["HelpCalloutBorderBrush"] = Color.FromRgb(0xBF, 0xD0, 0xE2)
        };

        public static string Toggle(string currentTheme)
        {
            return Normalize(currentTheme) == DarkTheme ? LightTheme : DarkTheme;
        }

        public static string Normalize(string theme)
        {
            return string.Equals(theme, LightTheme, StringComparison.OrdinalIgnoreCase)
                ? LightTheme
                : DarkTheme;
        }

        public static string Apply(string theme, ResourceDictionary resources)
        {
            string normalized = Normalize(theme);
            if (resources == null)
                return normalized;

            var palette = normalized == LightTheme ? LightPalette : DarkPalette;
            foreach (var item in palette)
                SetBrush(resources, item.Key, item.Value);

            return normalized;
        }

        private static bool SetBrush(ResourceDictionary resources, string key, Color color)
        {
            if (resources.Contains(key))
            {
                var brush = resources[key] as SolidColorBrush;
                if (brush != null && !brush.IsFrozen)
                {
                    brush.Color = color;
                }
                else
                {
                    resources[key] = new SolidColorBrush(color);
                }

                return true;
            }

            foreach (ResourceDictionary merged in resources.MergedDictionaries)
            {
                if (SetBrush(merged, key, color))
                    return true;
            }

            return false;
        }
    }
}
