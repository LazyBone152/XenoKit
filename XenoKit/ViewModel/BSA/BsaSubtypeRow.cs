using System.ComponentModel;
using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public enum BsaSubtypeKind
    {
        TimedType,
        Collision,
        Expiration
    }

    public class BsaSubtypeRow : INotifyPropertyChanged, System.IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public BsaSubtypeKind Kind { get; }
        public object Source { get; }
        public string StartTime => Source is IBsaType type ? type.StartTime.ToString() : string.Empty;
        public string Duration => Source is IBsaType type ? type.Duration.ToString() : string.Empty;
        public string DisplayType => GetDisplayType();
        public string DisplayName => displayName ?? DisplayType;
        public bool CanMove => Kind == BsaSubtypeKind.TimedType;
        public bool CanCopy => true;
        public bool CanDelete => true;
        private readonly string kindDisplayType;
        private readonly string displayName;

        public BsaSubtypeRow(IBsaType type)
        {
            Kind = BsaSubtypeKind.TimedType;
            Source = type;
            type.PropertyChanged += Type_PropertyChanged;
        }

        public BsaSubtypeRow(BSA_Collision collision, int index)
        {
            Kind = BsaSubtypeKind.Collision;
            Source = collision;
            kindDisplayType = GetCollisionDisplayType(collision);
            displayName = $"Collision {index}";
        }

        public BsaSubtypeRow(BSA_Expiration expiration, int index)
        {
            Kind = BsaSubtypeKind.Expiration;
            Source = expiration;
            kindDisplayType = "Expiration";
            displayName = $"Expiration {index}";
        }

        public void RefreshTiming()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartTime)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Duration)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayType)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
        }

        public void Dispose()
        {
            if (Source is IBsaType type)
                type.PropertyChanged -= Type_PropertyChanged;
        }

        private void Type_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IBsaType.StartTime) ||
                e.PropertyName == nameof(IBsaType.Duration) ||
                e.PropertyName == nameof(IBsaType.Type) ||
                string.IsNullOrEmpty(e.PropertyName))
            {
                RefreshTiming();
            }
        }

        private string GetDisplayType()
        {
            if (Source is IBsaType type)
                return type.Type;

            if (Source is BSA_Collision collision)
                return GetCollisionDisplayType(collision);

            return kindDisplayType;
        }

        private static string GetCollisionDisplayType(BSA_Collision collision)
        {
            return $"Collision ({collision.EepkType}, {collision.SkillID}, {collision.EffectID})";
        }
    }
}
