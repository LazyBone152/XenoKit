using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using XenoKit.Editor;
using Xv2CoreLib.Resource;

namespace XenoKit.Controls
{
    /// <summary>
    /// Interaction logic for Log.xaml
    /// </summary>
    public partial class LogView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public AsyncObservableCollection<LogEntry> LogEntries
        {
            get
            {
                return Log.Entries;
            }
        }

        #region Properties
        private LogEntry _selectedEntry = null;
        public LogEntry SelectedEntry
        {
            get
            {
                return _selectedEntry;
            }
            set
            {
                if(_selectedEntry != value)
                {
                    _selectedEntry = value;
                    NotifyPropertyChanged(nameof(SelectedEntry));
                }
            }
        }

        #endregion


        #region Commands
        public RelayCommand CopyLogEntryCommand => new RelayCommand(CopyLogEntry, CanCopyLogEntry);
        public void CopyLogEntry()
        {
            if (!CanCopyLogEntry()) return;

            string text = $"Severity: {_selectedEntry.Type}{Environment.NewLine}" +
                $"Occurrences: {_selectedEntry.Num}{Environment.NewLine}" +
                $"Message: {_selectedEntry.Message}";

            if (!string.IsNullOrWhiteSpace(_selectedEntry.Exception))
            {
                text += Environment.NewLine + Environment.NewLine +
                    "Details:" + Environment.NewLine + _selectedEntry.Exception;
            }

            Clipboard.SetText(text, TextDataFormat.Text);
        }

        public RelayCommand ClearAllCommand => new RelayCommand(ClearAll, CanClear);
        public void ClearAll()
        {
            Log.ClearAll();
        }

        private bool CanClear()
        {
            return LogEntries.Count > 0;
        }

        private bool CanCopyLogEntry()
        {
            return _selectedEntry != null;
        }


        #endregion

        public LogView()
        {
            InitializeComponent();
            DataContext = this;

            dataGrid.Items.SortDescriptions.Add(new SortDescription("Index", ListSortDirection.Descending));
        }

        public void SetSelectedEntry(LogEntry entry)
        {
            SelectedEntry = entry;
        }
    }
}
