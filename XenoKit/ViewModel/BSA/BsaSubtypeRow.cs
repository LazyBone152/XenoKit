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

    public class BsaSubtypeRow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public BsaSubtypeKind Kind { get; }
        public object Source { get; }
        public string StartTime => Source is IBsaType type ? type.StartTime.ToString() : string.Empty;
        public string Duration => Source is IBsaType type ? type.Duration.ToString() : string.Empty;
        public string DisplayType => Source is IBsaType type ? BsaTypeNames.GetName(type) : kindDisplayType;
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
        }

        public BsaSubtypeRow(BSA_Collision collision, int index)
        {
            Kind = BsaSubtypeKind.Collision;
            Source = collision;
            kindDisplayType = "Collision";
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
    }

    public static class BsaTypeNames
    {
        public static string GetName(object value)
        {
            if (value == null) return string.Empty;

            int typeId = GetTypeId(value);

            switch (typeId)
            {
                case 0:
                    return "Entry Passing";
                case 1:
                    return "Movement";
                case 2:
                    return "Type2";
                case 3:
                    return "Hitbox";
                case 4:
                    return "Deflection";
                case 6:
                    return "Effect";
                case 7:
                    return "Sound";
                case 8:
                    return "Screen Effect";
                case 10:
                    return "Unknown 10";
                case 12:
                    return "Unknown 12";
                case 13:
                    return "Unknown 13";
                case 14:
                    return "Unknown 14";
                default:
                    return typeId >= 0 ? "Unknown" : value.GetType().Name;
            }
        }

        public static int GetTypeId(object value)
        {
            string name = value.GetType().Name;
            const string prefix = "BSA_Type";
            if (!name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)) return -1;
            return int.TryParse(name.Substring(prefix.Length), out int typeId) ? typeId : -1;
        }
    }
}
