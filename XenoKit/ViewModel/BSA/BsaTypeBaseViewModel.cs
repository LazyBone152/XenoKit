using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.UndoRedo;
using YAXLib;

namespace XenoKit.ViewModel.BSA
{
    // Known BSA fields use typed viewmodels. This row is only for remaining unknown primitive fields while the format is being mapped.
    public class BsaFieldViewModel : ObservableObject
    {
        private readonly object source;
        private readonly PropertyInfo property;

        public string Name { get; }
        public bool IsEnum => property.PropertyType.IsEnum;
        public Array EnumValues => IsEnum ? Enum.GetValues(property.PropertyType) : null;

        public object SelectedEnumValue
        {
            get => property.GetValue(source, null);
            set
            {
                if (value == null || Equals(SelectedEnumValue, value)) return;
                SetValue(value);
            }
        }

        public string ValueText
        {
            get
            {
                object value = property.GetValue(source, null);
                if (value is float floatValue) return floatValue.ToString(CultureInfo.InvariantCulture);
                if (value is double doubleValue) return doubleValue.ToString(CultureInfo.InvariantCulture);
                return value?.ToString() ?? string.Empty;
            }
            set
            {
                if (TryParse(value, property.PropertyType, out object parsedValue))
                    SetValue(parsedValue);
                else
                    RaisePropertyChanged(nameof(ValueText));
            }
        }

        public BsaFieldViewModel(object source, PropertyInfo property, string name)
        {
            this.source = source;
            this.property = property;
            Name = name;
        }

        public void Refresh()
        {
            RaisePropertyChanged(nameof(ValueText));
            RaisePropertyChanged(nameof(SelectedEnumValue));
        }

        private void SetValue(object value)
        {
            object oldValue = property.GetValue(source, null);
            if (Equals(oldValue, value)) return;

            UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(property.Name, source, oldValue, value, $"BSA {Name}"));
            property.SetValue(source, value, null);
            Refresh();
        }

        private static bool TryParse(string value, Type type, out object parsedValue)
        {
            parsedValue = null;

            if (type == typeof(string))
            {
                parsedValue = value;
                return true;
            }

            if (type.IsEnum)
                return TryParseEnum(value, type, out parsedValue);

            string text = value?.Trim() ?? string.Empty;
            bool isHex = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
            string numberText = isHex ? text.Substring(2) : text;

            try
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Byte:
                        parsedValue = isHex ? Convert.ToByte(numberText, 16) : byte.Parse(numberText, CultureInfo.InvariantCulture);
                        return true;
                    case TypeCode.Int16:
                        parsedValue = isHex ? Convert.ToInt16(numberText, 16) : short.Parse(numberText, CultureInfo.InvariantCulture);
                        return true;
                    case TypeCode.UInt16:
                        parsedValue = isHex ? Convert.ToUInt16(numberText, 16) : ushort.Parse(numberText, CultureInfo.InvariantCulture);
                        return true;
                    case TypeCode.Int32:
                        parsedValue = isHex ? Convert.ToInt32(numberText, 16) : int.Parse(numberText, CultureInfo.InvariantCulture);
                        return true;
                    case TypeCode.UInt32:
                        parsedValue = isHex ? Convert.ToUInt32(numberText, 16) : uint.Parse(numberText, CultureInfo.InvariantCulture);
                        return true;
                    case TypeCode.Single:
                        parsedValue = float.Parse(numberText, CultureInfo.InvariantCulture);
                        return true;
                    case TypeCode.Double:
                        parsedValue = double.Parse(numberText, CultureInfo.InvariantCulture);
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool TryParseEnum(string value, Type type, out object parsedValue)
        {
            try
            {
                parsedValue = Enum.Parse(type, value);
                return true;
            }
            catch
            {
                parsedValue = null;
                return false;
            }
        }
    }

    public class BsaTypeBaseViewModel : ObservableObject, IDisposable
    {
        protected readonly IBsaType type;

        public string Title => BsaTypeNames.GetName(type);
        public ObservableCollection<BsaFieldViewModel> PrimaryFields { get; }
        public ObservableCollection<BsaFieldViewModel> UnknownFields { get; }
        public bool HasPrimaryFields => PrimaryFields.Count > 0;
        public bool HasUnknownFields => UnknownFields.Count > 0;
        public bool IsMovement => type is BSA_Type1;
        public bool IsHitbox => type is BSA_Type3;
        public bool IsEffect => type is BSA_Type6;
        protected virtual IReadOnlyCollection<string> TypedFieldNames => Array.Empty<string>();
        protected virtual IReadOnlyCollection<string> PrimaryFieldNames => Array.Empty<string>();
        protected virtual IReadOnlyDictionary<string, string> KnownFieldNames => EmptyFieldNames;
        private static readonly IReadOnlyDictionary<string, string> EmptyFieldNames = new Dictionary<string, string>();
        public event EventHandler TypeChanged;

        public ushort StartTime
        {
            get => type.StartTime;
            set => SetValue(nameof(type.StartTime), type.StartTime, value, "BSA Start Time");
        }

        public ushort Duration
        {
            get => type.Duration;
            set => SetValue(nameof(type.Duration), type.Duration, value, "BSA Duration");
        }

        protected BsaTypeBaseViewModel(IBsaType type)
        {
            this.type = type;
            PrimaryFields = new ObservableCollection<BsaFieldViewModel>(CreateRows(true));
            UnknownFields = new ObservableCollection<BsaFieldViewModel>(CreateRows(false));
            UndoManager.Instance.UndoOrRedoCalled += UndoManager_UndoOrRedoCalled;
        }

        public static BsaTypeBaseViewModel Create(IBsaType type)
        {
            switch (type)
            {
                case BSA_Type0 type0:
                    return new BsaType0ViewModel(type0);
                case BSA_Type1 type1:
                    return new BsaType1ViewModel(type1);
                case BSA_Type2 type2:
                    return new BsaType2ViewModel(type2);
                case BSA_Type3 type3:
                    return new BsaType3ViewModel(type3);
                case BSA_Type4 type4:
                    return new BsaType4ViewModel(type4);
                case BSA_Type6 type6:
                    return new BsaType6ViewModel(type6);
                case BSA_Type7 type7:
                    return new BsaType7ViewModel(type7);
                case BSA_Type8 type8:
                    return new BsaType8ViewModel(type8);
                case BSA_Type10 type10:
                    return new BsaType10ViewModel(type10);
                case BSA_Type12 type12:
                    return new BsaType12ViewModel(type12);
                case BSA_Type13 type13:
                    return new BsaType13ViewModel(type13);
                case BSA_Type14 type14:
                    return new BsaType14ViewModel(type14);
                default:
                    return new BsaTypeBaseViewModel(type);
            }
        }

        public virtual void Dispose()
        {
            UndoManager.Instance.UndoOrRedoCalled -= UndoManager_UndoOrRedoCalled;
        }

        protected void NotifyTypeChanged()
        {
            TypeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UndoManager_UndoOrRedoCalled(object sender, EventArgs e)
        {
            RaisePropertyChanged(string.Empty);
            foreach (BsaFieldViewModel field in PrimaryFields.Concat(UnknownFields))
                field.Refresh();
        }

        private void SetValue<T>(string propertyName, T oldValue, T newValue, string undoName)
        {
            if (Equals(oldValue, newValue)) return;

            UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(propertyName, type, oldValue, newValue, undoName));
            type.GetType().GetProperty(propertyName).SetValue(type, newValue, null);
            RaisePropertyChanged(propertyName);
            NotifyTypeChanged();
        }

        private IEnumerable<BsaFieldViewModel> CreateRows(bool primary)
        {
            return type.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(CanShowProperty)
                .Where(property => !TypedFieldNames.Contains(property.Name))
                .Where(property => primary == PrimaryFieldNames.Contains(property.Name))
                .Select(property => new BsaFieldViewModel(type, property, GetFieldName(property.Name)));
        }

        private static bool CanShowProperty(PropertyInfo property)
        {
            if (!property.CanRead || !property.CanWrite) return false;
            if (property.GetIndexParameters().Length > 0) return false;
            if (property.Name == nameof(IBsaType.StartTime) || property.Name == nameof(IBsaType.Duration)) return false;
            if (property.GetCustomAttributes(typeof(YAXDontSerializeAttribute), true).Length > 0) return false;
            Type type = property.PropertyType;
            return type == typeof(string) || type.IsEnum || type.IsPrimitive || type == typeof(decimal);
        }

        private string GetFieldName(string propertyName)
        {
            return KnownFieldNames.TryGetValue(propertyName, out string name) ? name : propertyName;
        }
    }

}
