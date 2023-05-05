using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xv2CoreLib;

namespace XenoKit.Windows
{
    /// <summary>
    /// Interaction logic for EntitySelector.xaml
    /// </summary>
    public partial class EntitySelector : MetroWindow
    {
        public ObservableCollection<Xv2Item> Items { get; set; }

        public Xv2Item SelectedItem { get; set; }
        public bool OnlyLoadFromCPK { get; set; }

        public EntitySelector(IEnumerable<Xv2Item> items, string itemName, Window parent)
        {
            Items = new ObservableCollection<Xv2Item>(items);
            InitializeComponent();
            DataContext = this;
            Owner = parent;
            Title = string.Format("Select {0}", itemName);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SelectedItem = listBox.SelectedItem as Xv2Item;
            Close();
        }

        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && (listBox.SelectedItem as Xv2Item) != null)
            {
                SelectedItem = listBox.SelectedItem as Xv2Item;
                Close();
            }
        }

        private void ListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Enter) && (listBox.SelectedItem as Xv2Item) != null)
            {
                e.Handled = true;
                SelectedItem = listBox.SelectedItem as Xv2Item;
                Close();
            }
        }
    }
}
