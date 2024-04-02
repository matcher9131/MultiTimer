using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MultiTimer.Converters
{
    public class TicksConverter : IValueConverter
    {
        // long(ticks) -> string
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not long ticks) throw new ArgumentException("Type of 'value' is invalid");

            var time = TimeSpan.FromTicks(ticks);
            return $"{(int)time.TotalHours:D}:{time.Minutes:D2}:{time.Seconds:D2}";
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
