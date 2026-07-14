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
            ["HelpCalloutBorderBrush"] = Color.FromRgb(0x33, 0x41, 0x55),
            ["CadCanvasBrush"] = Color.FromRgb(0x05, 0x0A, 0x14),
            ["HelpCodeBrush"] = Color.FromRgb(0x1F, 0x29, 0x37),
            ["HelpCodeBorderBrush"] = Color.FromRgb(0x47, 0x55, 0x69),
            ["HelpWarningTextBrush"] = Color.FromRgb(0xFD, 0xE6, 0x8A)
        };

        private static readonly Dictionary<string, Color> LightPalette = new Dictionary<string, Color>
        {
            ["BgBrush"] = Color.FromRgb(0xE3, 0xEA, 0xF2),
            ["PanelBrush"] = Color.FromRgb(0xFF, 0xFF, 0xFF),
            ["PanelAltBrush"] = Color.FromRgb(0xED, 0xF3, 0xF8),
            ["BorderBrush"] = Color.FromRgb(0x8F, 0xA4, 0xBB),
            ["BorderSoftBrush"] = Color.FromRgb(0x5F, 0x75, 0x8D),
            ["TextBrush"] = Color.FromRgb(0x10, 0x20, 0x33),
            ["MutedBrush"] = Color.FromRgb(0x42, 0x59, 0x70),
            ["AccentBrush"] = Color.FromRgb(0x00, 0x7A, 0x91),
            ["DataGridRowBrush"] = Color.FromRgb(0xFF, 0xFF, 0xFF),
            ["DataGridAltRowBrush"] = Color.FromRgb(0xF7, 0xFA, 0xFD),
            ["DataGridHeaderBrush"] = Color.FromRgb(0xC7, 0xD7, 0xE8),
            ["HoverBrush"] = Color.FromRgb(0xCB, 0xEA, 0xF2),
            ["SelectedBrush"] = Color.FromRgb(0xA9, 0xD9, 0xE4),
            ["CardHeaderBrush"] = Color.FromRgb(0xD4, 0xE4, 0xF3),
            ["CardHeaderTextBrush"] = Color.FromRgb(0x0C, 0x25, 0x40),
            ["HelpCalloutBrush"] = Color.FromRgb(0xF5, 0xF8, 0xFC),
            ["HelpCalloutBorderBrush"] = Color.FromRgb(0xBF, 0xD0, 0xE2),
            ["CadCanvasBrush"] = Color.FromRgb(0xF8, 0xFA, 0xFC),
            ["HelpCodeBrush"] = Color.FromRgb(0xF1, 0xF5, 0xF9),
            ["HelpCodeBorderBrush"] = Color.FromRgb(0xCB, 0xD5, 0xE1),
            ["HelpWarningTextBrush"] = Color.FromRgb(0x9A, 0x34, 0x12)
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

        public static string Apply(string theme, ResourceDictionary resources, DependencyObject root)
        {
            string normalized = Apply(theme, resources);
            if (root != null)
                ApplyToVisualTree(normalized, root);

            return normalized;
        }

        private static void ApplyToVisualTree(string theme, DependencyObject node)
        {
            if (node is FrameworkElement element && element.Resources != null)
                Apply(theme, element.Resources);

            int childCount = VisualTreeHelper.GetChildrenCount(node);
            for (int index = 0; index < childCount; index++)
                ApplyToVisualTree(theme, VisualTreeHelper.GetChild(node, index));
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
