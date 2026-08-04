using System;
using System.ComponentModel;
using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    /// <summary>
    /// One row in the subtype list. Collision and Expiration are listed alongside the IBsaTypes as their own
    /// types, but they are stored in separate arrays and have no timing fields, so this adapts all three to a
    /// single set of columns. It forwards the source's own PropertyChanged, so rows stay live without the
    /// grid being refreshed.
    /// </summary>
    public class BsaSubtypeRow : INotifyPropertyChanged, IDisposable
    {
        private readonly INotifyPropertyChanged notifySource;

        public event PropertyChangedEventHandler PropertyChanged;

        public object Source { get; }

        public bool HasTiming => Source is IBsaType;

        public string StartTime => Source is IBsaType type ? type.StartTime.ToString() : string.Empty;
        public string Duration => Source is IBsaType type ? type.Duration.ToString() : string.Empty;

        public string Type
        {
            get
            {
                switch (Source)
                {
                    case IBsaType type:
                        return type.Type;
                    case BSA_Collision collision:
                        return $"Collision ({collision.EepkType}, {collision.SkillID}, {collision.EffectID})";
                    case BSA_Expiration expiration:
                        return $"Collision Sound ({expiration.I_00}, {expiration.I_02}, {expiration.I_04}, {expiration.I_06})";
                    default:
                        return string.Empty;
                }
            }
        }

        public BsaSubtypeRow(object source)
        {
            Source = source;
            notifySource = source as INotifyPropertyChanged;

            if (notifySource != null)
                notifySource.PropertyChanged += Source_PropertyChanged;
        }

        public void Dispose()
        {
            if (notifySource != null)
                notifySource.PropertyChanged -= Source_PropertyChanged;
        }

        private void Source_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Refresh();
        }

        public void Refresh()
        {
            NotifyPropertyChanged(nameof(StartTime));
            NotifyPropertyChanged(nameof(Duration));
            NotifyPropertyChanged(nameof(Type));
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
