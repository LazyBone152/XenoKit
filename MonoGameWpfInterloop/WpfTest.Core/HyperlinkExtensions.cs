using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;

namespace WpfTest
{
    /// <summary>
    /// Extension that allows launching a hyperlink via click without having to create a custom event handler for every single hyperlink element.
    /// From: http://stackoverflow.com/a/11433814
    /// </summary>
    public static class HyperlinkExtensions
    {
        public static bool GetLaunchInBrowser(DependencyObject obj)
        {
            return (bool)obj.GetValue(LaunchInBrowserProperty);
        }

        public static void SetLaunchInBrowser(DependencyObject obj, bool value)
        {
            obj.SetValue(LaunchInBrowserProperty, value);
        }
        public static readonly DependencyProperty LaunchInBrowserProperty = DependencyProperty.RegisterAttached("LaunchInBrowser", typeof(bool), typeof(HyperlinkExtensions), new UIPropertyMetadata(false, OnIsChanged));

        private static void OnIsChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            var hyperlink = sender as Hyperlink;
            if (hyperlink == null)
                return;

            if ((bool)args.NewValue)
                hyperlink.RequestNavigate += Hyperlink_RequestNavigate;
            else
                hyperlink.RequestNavigate -= Hyperlink_RequestNavigate;
        }

        private static void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}