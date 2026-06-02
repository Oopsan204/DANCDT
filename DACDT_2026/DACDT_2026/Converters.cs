using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace DACDT_2026
{
    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool flag && flag ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Visibility visibility && visibility == Visibility.Visible;
    }

    public sealed class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool flag && flag ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Visibility visibility && visibility != Visibility.Visible;
    }

    public sealed class BoolToStatusBrushConverter : IValueConverter
    {
        public Brush TrueBrush { get; set; } = Brushes.LimeGreen;
        public Brush FalseBrush { get; set; } = Brushes.IndianRed;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool flag && flag ? TrueBrush : FalseBrush;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    public sealed class InverseZoomStrokeThicknessConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double thickness = ToDouble(values, 0, 1.0);
            double zoom = Math.Max(ToDouble(values, 1, 1.0), 0.001);

            return Math.Max(thickness / zoom, 0.05);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => null;

        private static double ToDouble(object[] values, int index, double fallback)
        {
            if (values == null || values.Length <= index || values[index] == null)
                return fallback;

            try
            {
                return System.Convert.ToDouble(values[index], CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }
    }
}
