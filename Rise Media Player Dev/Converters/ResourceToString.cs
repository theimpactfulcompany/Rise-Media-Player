using Rise.Common.Extensions.Markup;
using System;
using Windows.UI.Xaml.Data;

namespace Rise.App.Converters
{
    public sealed class ResourceToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string key = value?.ToString();
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            if (parameter != null)
            {
                string loader = parameter.ToString();
                return ResourceHelper.GetString($"/{loader}/{key}");
            }

            return ResourceHelper.GetString(key);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotSupportedException();
    }
}
