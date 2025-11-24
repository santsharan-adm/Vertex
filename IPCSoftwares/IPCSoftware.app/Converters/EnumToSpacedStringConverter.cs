using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace IPCSoftware.App.Converters
{
    public class EnumToSpacedStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            var str = value.ToString();
            if (string.IsNullOrEmpty(str)) return string.Empty;

            var builder = new StringBuilder();
            builder.Append(str[0]);

            for (int i = 1; i < str.Length; i++)
            {
                // Current char
                var current = str[i];
                // Previous char
                var previous = str[i - 1];
                // Next char (or null if end)
                char? next = i + 1 < str.Length ? str[i + 1] : (char?)null;

                if (char.IsUpper(current))
                {
                    // Add space if previous is lowercase or next is lowercase (start of new word)
                    if (char.IsLower(previous) || (next.HasValue && char.IsLower(next.Value)))
                    {
                        builder.Append(' ');
                    }
                }

                builder.Append(current);
            }

            return builder.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
