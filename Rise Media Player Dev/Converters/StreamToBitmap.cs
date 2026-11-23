using Rise.Common.Extensions;
using System;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace Rise.App.Converters
{
    /// <summary>
    /// Creates asynchronously loaded bitmap images from random access streams.
    /// </summary>
    public sealed class StreamToBitmap : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
                return null;

            IRandomAccessStream strm;
            if (value is IRandomAccessStream rand)
                strm = rand.CloneStream();
            else if (value is IRandomAccessStreamReference randRef)
                strm = randRef.OpenReadAsync().Get();
            else
                return null;

            var img = new BitmapImage();
            void Cleanup(object sender, RoutedEventArgs e)
            {
                img.ImageOpened -= Cleanup;
                img.ImageFailed -= CleanupFailed;
                strm.Dispose();
            }

            void CleanupFailed(object sender, ExceptionRoutedEventArgs e)
            {
                img.ImageOpened -= Cleanup;
                img.ImageFailed -= CleanupFailed;
                strm.Dispose();
            }

            img.ImageOpened += Cleanup;
            img.ImageFailed += CleanupFailed;

            _ = img.SetSourceAsync(strm);
            return img;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
