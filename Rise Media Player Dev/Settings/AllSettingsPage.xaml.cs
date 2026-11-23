using Rise.App.Views;
using Rise.Common.Constants;
using Rise.Common.Extensions;
using Rise.Common.Extensions.Markup;
using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Rise.App.Settings
{
    public sealed partial class AllSettingsPage : Page
    {
        public AllSettingsPage()
        {
            InitializeComponent();

            TitleBar.SetTitleBarForCurrentView();

            SettingsMainFrame.Navigated += (_, _) => FinishNavigation();
            _ = SettingsMainFrame.Navigate(typeof(AppearanceBasePage));
        }

        private async void FeedbackSettings_Click(object sender, RoutedEventArgs e)
        {
            await URLs.NewIssue.LaunchAsync();
        }

        private void Insider_Click(object sender, RoutedEventArgs e)
        {
            _ = SettingsMainFrame.Navigate(typeof(InsiderPage));
        }

        private void Language_Click(object sender, RoutedEventArgs e)
        {
            _ = SettingsMainFrame.Navigate(typeof(LanguagePage));
        }

        private void SettingsSidebar_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            string tag = args?.InvokedItemContainer?.Tag?.ToString();
            if (tag != null)
            {
                Type page = tag switch
                {
                    "Appearance" => typeof(AppearanceBasePage),
                    "MediaLibrary" => typeof(MediaLibraryBasePage),
                    "Navigation" => typeof(NavigationPage),
                    "Playback" => typeof(PlaybackPage),
                    "Behaviour" => typeof(WindowsBehavioursPage),
                    "About" => typeof(AboutPage),
                    _ => typeof(MediaLibraryBasePage),
                };

                if (SettingsMainFrame.CurrentSourcePageType != page)
                {
                    _ = SettingsMainFrame.Navigate(page, null, args.RecommendedNavigationTransitionInfo);
                    SettingsMainFrame.BackStack.Clear();
                }
            }
        }

        private void FinishNavigation()
        {
            string tag = SettingsMainFrame.CurrentSourcePageType?.Name;
            switch (tag)
            {
                case "AppearanceBasePage":
                    MainSettingsHeaderIcon.Glyph = "\uE771";
                    MainSettingsHeader.Text = ResourceHelper.GetString("Appearance");
                    break;
                case "MediaLibraryBasePage":
                    MainSettingsHeaderIcon.Glyph = "\uEA69";
                    MainSettingsHeader.Text = ResourceHelper.GetString("MediaLibrary");
                    break;
                case "MediaSourcesPage":
                    MainSettingsHeaderIcon.Glyph = "\uE838";
                    MainSettingsHeader.Text = ResourceHelper.GetString("/Settings/MediaLibraryManageFoldersTitle");
                    break;
                case "NavigationPage":
                    MainSettingsHeaderIcon.Glyph = "\uE8B0";
                    MainSettingsHeader.Text = ResourceHelper.GetString("Navigation");
                    break;
                case "PlaybackPage":
                    MainSettingsHeaderIcon.Glyph = "\uF4C3";
                    MainSettingsHeader.Text = ResourceHelper.GetString("PlaybackAndSound");
                    break;
                case "ComingSoonPage":
                    MainSettingsHeaderIcon.Glyph = "\uE115";
                    MainSettingsHeader.Text = ResourceHelper.GetString("ComingSoon");
                    break;
                case "WindowsBehavioursPage":
                    MainSettingsHeaderIcon.Glyph = "\uEC7A";
                    MainSettingsHeader.Text = ResourceHelper.GetString("SystemBehaviors");
                    break;
                case "AboutPage":
                    MainSettingsHeaderIcon.Glyph = "\uE946";
                    MainSettingsHeader.Text = ResourceHelper.GetString("About");
                    break;
                case "InsiderPage":
                    MainSettingsHeaderIcon.Glyph = "\uF1AD";
                    MainSettingsHeader.Text = ResourceHelper.GetString("InsiderHub");
                    break;
                case "LanguagePage":
                    MainSettingsHeaderIcon.Glyph = "\uE12B";
                    MainSettingsHeader.Text = ResourceHelper.GetString("Language");
                    break;
            }
        }

        private void GoBackAPage_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsMainFrame.CanGoBack)
                SettingsMainFrame.GoBack();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
            else
                _ = Frame.Navigate(typeof(MainPage));
        }
    }
}