using Rise.Common.Extensions.Markup;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Data;

namespace Rise.App.Converters
{
    public sealed partial class TimeSpanToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var span = value is TimeSpan time ? time : TimeSpan.Zero;

            string param = parameter?.ToString();
            if (string.IsNullOrEmpty(param))
                return GetShortFormat(span);

            return GetLongFormat(ref span, param);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    // Converter logic
    public sealed partial class TimeSpanToString
    {
        public static string GetLongFormat(TimeSpan value, string format)
            => GetLongFormat(ref value, format);

        public static string GetShortFormat(TimeSpan span)
            => GetShortFormat(ref span);

        private static string GetLongFormat(ref TimeSpan value, string format)
        {
            if (string.IsNullOrEmpty(format))
                return string.Empty;

            char startUnit = format[0];
            char endUnit = format.Length >= 3 ? format[2] : startUnit;

            var parts = new List<string>(4);
            void AppendUnit(string resource, int count)
            {
                if (count > 0)
                    parts.Add(ResourceHelper.GetLocalizedCount(resource, count));
            }

            switch (startUnit)
            {
                case 'D':
                    AppendUnit("Day", value.Days);
                    if (endUnit != 'D') goto case 'H';
                    break;

                case 'H':
                    AppendUnit("Hour", value.Hours);
                    if (endUnit != 'H') goto case 'M';
                    break;

                case 'M':
                    AppendUnit("Minute", value.Minutes);
                    if (endUnit != 'M') goto case 'S';
                    break;

                case 'S':
                    AppendUnit("Second", value.Seconds);
                    break;
            }

            return string.Join(", ", parts);
        }

        private static string GetShortFormat(ref TimeSpan span)
        {
            if (span.Days >= 1)
                return span.ToString("d.\\hh\\:mm\\:ss");
            if (span.Hours >= 1 && span.Hours <= 9)
                return span.ToString("h\\:mm\\:ss");
            if (span.Hours >= 10)
                return span.ToString("hh\\:mm\\:ss");
            if (span.Minutes >= 10)
                return span.ToString("mm\\:ss");

            return span.ToString("m\\:ss");
        }
    }
}
