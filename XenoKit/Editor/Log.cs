using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Xv2CoreLib.Resource;

namespace XenoKit.Editor
{
    public static class Log
    {
        public static AsyncObservableCollection<LogEntry> Entries { get; set; } = new AsyncObservableCollection<LogEntry>();

        public static void Add(string message)
        {
            Add(message, LogType.Info);
        }

        public static void Add(string message, LogType type)
        {
            int existing = Exists(message, type);

            if (existing != -1)
            {
                PushToTop(existing);
                LogEntryAddedEvent.Invoke(Entries[existing], null);
                return;
            }

            var newEntry = new LogEntry(message, type, Entries.Count);
            Entries.Add(newEntry);
            LogEntryAddedEvent?.Invoke(newEntry, null);
        }

        public static void Add(string message, string exception, LogType type)
        {
            int existing = Exists(message, type);

            if (existing != -1)
            {
                PushToTop(existing);
                LogEntryAddedEvent.Invoke(Entries[existing], null);
                return;
            }

            var newEntry = new LogEntry(message, exception, type, Entries.Count);
            Entries.Add(newEntry);
            LogEntryAddedEvent?.Invoke(newEntry, null);
        }

        private static void PushToTop(int oldIdx)
        {
            Entries.Move(oldIdx, 0);
            Entries[0].Num++;
            RecalculateIndexes();
        }

        private static int Exists(string Message, LogType type)
        {
            for (int i = 0; i < Entries.Count; i++)
                if (Entries[i].Message == Message && Entries[i]._type == type) return i;

            return -1;
        }

        public static void ClearAll()
        {
            Task.Run(() =>
            {
                Entries.Clear();
                LogEntryAddedEvent?.Invoke(null, null);
            });
        }

        private static void RecalculateIndexes()
        {
            for(int i = 0; i < Entries.Count; i++)
            {
                Entries[i].Index = i;
            }
        }

        public static event EventHandler LogEntryAddedEvent;
    }

    public class LogContainer
    {
        public AsyncObservableCollection<LogEntry> Entries { get; set; } = new AsyncObservableCollection<LogEntry>();
    }

    public class LogEntry : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        public string _message;
        public string Exception;
        public LogType _type;
        
        public string Message { get { return _message; } }
        public string Type { get { return _type.ToString(); } }

        private int _num = 1;
        public int Num
        {
            get { return _num; }
            set
            {
                if(_num != value)
                {
                    _num = value;
                    NotifyPropertyChanged(nameof(Num));
                }
            }
        }

        private int _index = 0;
        public int Index
        {
            get { return _index; }
            set
            {
                if (_index != value)
                {
                    _index = value;
                    NotifyPropertyChanged(nameof(Index));
                }
            }
        }

        public LogEntry(string message, LogType type, int index)
        {
            _message = message;
            _type = type;
            Exception = null;
            Index = index;
        }

        public LogEntry(string message, string exception, LogType type, int index)
        {
            _message = message;
            _type = type;
            Exception = exception;
            Index = index;
        }
    }

    public enum LogType
    {
        Info,
        Warning,
        Error
    }
}
