using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace XenoKit.Helper
{
    public static class ViewHelpers
    {
        public static string GetMenuItemString(RoutedEventArgs e)
        {
            MenuItem selectedMenuItem = e.OriginalSource as MenuItem;

            if (selectedMenuItem != null)
            {
                return selectedMenuItem.DataContext as string;
            }

            return null;
        }

        public static void GetDpiScalingFactor(out float x, out float y)
        {
            var dpiXProperty = typeof(SystemParameters).GetProperty("DpiX", BindingFlags.NonPublic | BindingFlags.Static);
            var dpiYProperty = typeof(SystemParameters).GetProperty("Dpi", BindingFlags.NonPublic | BindingFlags.Static);

            int dpiX = (int)dpiXProperty.GetValue(null, null);
            int dpiY = (int)dpiYProperty.GetValue(null, null);

            x = dpiX / 96f;
            y = dpiY / 96f;
        }
    }
}
