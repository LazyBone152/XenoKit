using System;
using System.Windows;
using System.Windows.Controls;

namespace WpfTest.UserControls
{
    public class CloseableTabItem : TabItem
    {
        public CloseableTabItem(string title, Action<CloseableTabItem> closeRequested)
        {
            var h = new CloseableHeader { TabTitle = { Content = title } };
            h.Close.Click += (sender, args) =>
            {
                closeRequested(this);
            };
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;
            Header = h;

        }

        /// <summary>
        /// Invoked when a tab is removed.
        /// </summary>
        public event EventHandler Closed;

        public void Close()
        {
            Closed?.Invoke(this, EventArgs.Empty);
            Closed = null;
        }
    }
}