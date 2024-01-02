using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
namespace XenoKit.Windows
{
    /// <summary>
    /// Interaction logic for DiscreteSelector.xaml
    /// </summary>
    public partial class DiscreteSelector : MetroWindow
    {
        public List<SelectorItem> Items { get; private set; }
        public object Context { get; private set; }

        public event DiscreteSelectorCallbackEventHandler SelectionFinishedCallback;

        private bool WindowPosSet = false;
        private double WindowPosLeft;
        private double WindowPosTop;

        private bool WasConfirmedButtonPressed = false;

        public DiscreteSelector(List<SelectorItem> items, object context, int height = -1, string titleBarText = null)
        {
            Items = items;
            Context = context;
            DataContext = this;
            InitializeComponent();

            double titleBarHeight = 0;

            if (!string.IsNullOrWhiteSpace(titleBarText))
            {
                titleText.Text = titleBarText;
                titleText.Visibility = Visibility.Visible;
                titleBarHeight = titleText.Height;
            }

            if (height > 0)
                Height = height;

            dataGrid.MaxHeight = Height - 50 - titleBarHeight;
        }

        private void SetWindowLocation()
        {
            Window mainWindow = Application.Current.MainWindow;
            var mousePos = mainWindow.PointToScreen(Mouse.GetPosition(mainWindow));
            Helper.ViewHelpers.GetDpiScalingFactor(out float xScale, out float yScale);
            Top = mousePos.Y / yScale;
            Left = mousePos.X / xScale;
            WindowPosLeft = Left;
            WindowPosTop = Top;
            WindowPosSet = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            if(SelectionFinishedCallback != null)
            {
                SelectionFinishedCallback.Invoke(this, new DiscreteSelectorCallbackEventArgs(Items, Context, WasConfirmedButtonPressed));
                SelectionFinishedCallback = null;
            }

            base.OnClosed(e);
        }

        protected override void OnDeactivated(EventArgs e)
        {
            TryClose();
            base.OnDeactivated(e);
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);

            if((Top != WindowPosTop || Left != WindowPosLeft) && WindowPosSet)
                TryClose();
        }

        private void CloseEvent(object sender, RoutedEventArgs e)
        {
            TryClose();
        }

        private void TryClose()
        {
            try
            {
                Close();
            }
            catch { }
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SetWindowLocation();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if(((CheckBox)sender).DataContext is SelectorItem selectorItem)
            {
                if (dataGrid.SelectedItems.Contains(selectorItem))
                {
                    foreach (SelectorItem item in dataGrid.SelectedItems.Cast<SelectorItem>())
                    {
                        item.IsSelected = true;
                    }
                }
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (((CheckBox)sender).DataContext is SelectorItem selectorItem)
            {
                if (dataGrid.SelectedItems.Contains(selectorItem))
                {
                    foreach (SelectorItem item in dataGrid.SelectedItems.Cast<SelectorItem>())
                    {
                        item.IsSelected = false;
                    }
                }
            }
        }

        public RelayCommand SelectItemCommand => new RelayCommand(SelectItem);
        private void SelectItem()
        {
            if (dataGrid.SelectedItem == null) return;
            List<SelectorItem> selectItems = dataGrid.SelectedItems.Cast<SelectorItem>().ToList();

            if (selectItems.All(x => x.IsSelected))
            {
                foreach (SelectorItem item in selectItems)
                {
                    item.IsSelected = false;
                }
            }
            else
            {
                foreach (SelectorItem item in selectItems)
                {
                    item.IsSelected = true;
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            WasConfirmedButtonPressed = true;
            CloseEvent(sender, e);
        }
    }

    public class SelectorItem : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public int ID { get; private set; }
        public string Name { get; private set; }

        private bool _isSelected = true;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if(value !=  _isSelected)
                {
                    _isSelected = value;
                    NotifyPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public SelectorItem(int ID, string Name)
        {
            this.ID = ID;
            this.Name = Name;
            _isSelected = true;
        }

        public SelectorItem(int ID, string Name, bool IsSelected)
        {
            this.ID = ID;
            this.Name = Name;
            _isSelected = IsSelected;
        }
    }

    public delegate void DiscreteSelectorCallbackEventHandler(object source, DiscreteSelectorCallbackEventArgs e);

    public class DiscreteSelectorCallbackEventArgs : EventArgs
    {
        public List<SelectorItem> Items { get; private set; }
        public object Context { get; private set; }
        public bool WasConfirmationButtonPressed { get; private set; }

        public DiscreteSelectorCallbackEventArgs(List<SelectorItem> items, object context, bool wasConfirmationButtonPressed)
        {
            Items = items;
            Context = context;
            WasConfirmationButtonPressed = wasConfirmationButtonPressed;
        }
    }
}
