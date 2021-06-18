using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Data;

namespace ImageProcessingWPF.Converters
{
    class StringMatchConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length < 2)
                return false;

            for (int i = 1; i < values.Length; i++)
            {
                if (!(values[0] as string).Equals(values[i] as string))
                    return false;
            }

            return true;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
