using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using WpfTest.Scenes;
using WpfTest.UserControls;

namespace WpfTest.Views
{
    /// <summary>
    /// Interaction logic for CloseableTabWindow.xaml
    /// </summary>
    public partial class CloseableTabWindow : ILogToUi
    {
        private ObservableCollection<CloseableTabItem> _activeTabs;

        public CloseableTabWindow()
        {
            InitializeComponent();
            DataContext = this;
            ActiveTabs = new ObservableCollection<CloseableTabItem>();
            ActiveTabs.CollectionChanged += ActiveTabsOnCollectionChanged;
        }

        private void ActiveTabsOnCollectionChanged(object o, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var i in e.NewItems)
                {
                    Log("Added new tab.");
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var i in e.OldItems)
                {
                    var tab = (CloseableTabItem)i;
                    Log("Removed tab.");
                    tab.Close();
                }
            }
        }

        public ObservableCollection<CloseableTabItem> ActiveTabs
        {
            get { return _activeTabs; }
            set
            {
                _activeTabs = value;
                OnPropertyChanged(nameof(ActiveTabs));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Log(string message)
        {
            var now = DateTime.Now.TimeOfDay;
            LogOutput.AppendText($"{now}: {message}{Environment.NewLine}");
            LogOutput.ScrollToEnd();
        }

        private void AddNew_OnClick(object sender, RoutedEventArgs e)
        {
            AddNewTab();
        }

        private void AddNewTab()
        {
            var tab = new CloseableTabItem("Game tab", sender =>
            {
                ActiveTabs.Remove(sender);
            })
            {
                Content = new TabScene
                {
                    Margin = new Thickness(10)
                }
            };
            tab.Closed += (sender, args) =>
            {
                Log("tab closing event called.");
                var t = (CloseableTabItem)sender;
                var scene = (TabScene)t.Content;
                scene.Dispose();
            };
            ActiveTabs.Add(tab);
            tab.Focus();
        }

        private void CloseAll_OnClick(object sender, RoutedEventArgs e)
        {
            for (var i = 0; i < ActiveTabs.Count; i++)
            {
                var t = ActiveTabs[i];
                ActiveTabs.Remove(t);
                i--;
            }
        }
    }
}
