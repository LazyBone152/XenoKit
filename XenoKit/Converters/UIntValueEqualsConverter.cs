using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace XenoKit.Converters
{
    /// <summary>
    /// Binds a RadioButton to one value of a masked field. IsChecked is true when the bound value equals
    /// the ConverterParameter. Checking the radio writes that parameter back, unchecking writes nothing,
    /// which is what lets a group of radios share a single underlying value.
    /// </summary>
    public class UIntValueEqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;

            return TryParse(value, out uint current) && TryParse(parameter, out uint expected) && current == expected;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool isChecked) || !isChecked)
                return Binding.DoNothing;

            return TryParse(parameter, out uint expected) ? expected : (object)Binding.DoNothing;
        }

        private static bool TryParse(object value, out uint result)
        {
            result = 0;

            if (value is uint uintValue)
            {
                result = uintValue;
                return true;
            }

            string text = value as string ?? System.Convert.ToString(value, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(text)) return false;

            text = text.Trim();

            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return uint.TryParse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);

            return uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }
    }
}
