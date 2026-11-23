using Windows.UI.Xaml;

namespace Rise.Common.Helpers
{
    public static class BindingHelpers
    {
        public static Visibility BoolToVis(bool boolean)
            => boolean ? Visibility.Visible : Visibility.Collapsed;

        public static Visibility InverseBoolToVis(bool boolean)
            => !boolean ? Visibility.Visible : Visibility.Collapsed;

        public static Visibility IsNotNullVis(object obj)
            => BoolToVis(obj != null);

        public static bool Reverse(bool boolean)
            => !boolean;
    }
}
