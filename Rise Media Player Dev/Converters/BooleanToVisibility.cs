using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Rise.App.Converters
{
    public sealed class BooleanToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isTrue = value is bool b && b;
            bool reverse = parameter is string param && param is "Reverse" or "0";
            return (reverse ? !isTrue : isTrue) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            bool isVisible = value is Visibility v && v == Visibility.Visible;
            bool reverse = parameter is string param && param is "Reverse" or "0";
            return reverse ? !isVisible : isVisible;
        }
    }
}
