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
    }
}
