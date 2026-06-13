using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using XenoKit.Helper.Find;
using Xv2CoreLib.BAC;
using LB_Common;
using static Xv2CoreLib.Xenoverse2;
using XenoKit.Editor;
using Xv2CoreLib.Resource.UndoRedo;
using GalaSoft.MvvmLight.CommandWpf;
using Xv2CoreLib.BCM;

namespace XenoKit.Windows
{
    /// <summary>
    /// Interaction logic for FindAndReplace.xaml
    /// </summary>
    public partial class FindAndReplace : MetroWindow, INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
        
        public Dictionary<MoveFileTypes, string> FileTypes { get; private set; } = new Dictionary<MoveFileTypes, string>()
        {
            { MoveFileTypes.BAC , "BAC" },
            { MoveFileTypes.BCM , "BCM" }
        };

        public Dictionary<Type, string> BacTypes { get; private set; } = new Dictionary<Type, string>()
        {
            { typeof(BAC_Type0) , "Animation" },
            { typeof(BAC_Type1) , "Hitbox" },
            { typeof(BAC_Type2) , "Movement" },
            { typeof(BAC_Type3) , "Invulnerability" },
            { typeof(BAC_Type4) , "Time Scale" },
            { typeof(BAC_Type5) , "Tracking" },
            { typeof(BAC_Type6) , "Charge Control" },
            { typeof(BAC_Type7) , "BCM Callback" },
            { typeof(BAC_Type8) , "Effect" },
            { typeof(BAC_Type9) , "Projectile" },
            { typeof(BAC_Type10) , "Camera" },
            { typeof(BAC_Type11) , "Sound" },
            { typeof(BAC_Type12) , "Targeting Assistance" },
            { typeof(BAC_Type13) , "BCS Part Visibility" },
            { typeof(BAC_Type14) , "Bone Modification" },
            { typeof(BAC_Type15) , "Functions" },
            { typeof(BAC_Type16) , "Post Effect" },
            { typeof(BAC_Type17) , "Throw Handler" },
            { typeof(BAC_Type18) , "Physics Object" },
            { typeof(BAC_Type19) , "Aura" },
            { typeof(BAC_Type20) , "Homing Movement" },
            { typeof(BAC_Type21) , "Eye Movement" },
            { typeof(BAC_Type22) , "BAC_Type22" },
            { typeof(BAC_Type23) , "Transparency Effect" },
            { typeof(BAC_Type24) , "Dual Skill Handler" },
            { typeof(BAC_Type25) , "Extended Chain Attack" },
            { typeof(BAC_Type26) , "Extended Camera Control" },
            { typeof(BAC_Type27) , "Effect Property Control" },
            { typeof(BAC_Type28) , "BAC_Type28" },
            { typeof(BAC_Type29) , "BAC_Type29" },
            { typeof(BAC_Type30) , "BAC_Type30" },
        };

        //When adding types, remember to edit ConvertToType method!
        public readonly Type[] AllowedEnums = { typeof(BoneLinks), typeof(BAC_Type0.EanTypeEnum), typeof(BAC_Type10.EanTypeEnum), typeof(AcbType), typeof(AuraType), typeof(BcsPartId), typeof(TargetingAxis), typeof(BAC_Type8.EepkTypeEnum), typeof(BAC_Type20.HomingType), typeof(BAC_Type1.BoundingBoxTypeEnum) };

        MainWindow mainWindow;

        //Values
        private MoveFileTypes _selectedFileType = MoveFileTypes.BAC;
        private Type _selectedBacType = typeof(BAC_Type0);
        private Value _selectedValue;
        private bool _replaceMode = false;
        private List<Value> _values = new List<Value>();
        private string valueToFind = string.Empty;
        private string valueToReplace = string.Empty;
        private object prevFoundItem = null;

        //Props
        public MoveFileTypes SelectedFileType
        {
            get => _selectedFileType;
            set
            {
                if(value != _selectedFileType)
                {
                    _selectedFileType = value;
                    NotifyPropertyChanged(nameof(SelectedFileType));
                    NotifyPropertyChanged(nameof(Values));
                    NotifyPropertyChanged(nameof(BacTypeVisibility));
                    ResetState();

                    if (Values.Count > 0)
                        SelectedValue = Values[0];
                }
            }
        }
        public Type SelectedBacType
        {
            get => _selectedBacType;
            set
            {
                if (value != _selectedBacType)
                {
                    _selectedBacType = value;
                    NotifyPropertyChanged(nameof(SelectedBacType));
                    NotifyPropertyChanged(nameof(Values));
                    ResetState();

                    if (_values.Count > 0)
                        SelectedValue = _values[0];
                }
            }
        }
        public Value SelectedValue
        {
            get => _selectedValue;
            set
            {
                _selectedValue = value;
                NotifyPropertyChanged(nameof(SelectedValue));
                NotifyPropertyChanged(nameof(ValueToolTip));
                ResetState();
            }
        }

        public List<Value> Values
        {
            get
            {
                if (SelectedFileType == MoveFileTypes.BCM)
                {
                    _values = CreateBcmValues();
                    return _values;
                }

                _values = Find.ParseAllProps(SelectedBacType);

                //Remove all not-supported enums
                _values.RemoveAll(x => !AllowedEnums.Contains(x.valueType) && x.valueType.IsEnum);

                return _values;
            }
        }

        public string ValueToFind
        {
            get => valueToFind;
            set
            {
                if (value != valueToFind)
                {
                    valueToFind = value;
                    NotifyPropertyChanged(nameof(ValueToFind));
                    ResetState();
                }
            }
        }
        public string ValueToReplace
        {
            get => valueToReplace;
            set
            {
                if (value != valueToReplace)
                {
                    valueToReplace = value;
                    NotifyPropertyChanged(nameof(ValueToReplace));
                    ResetState();
                }
            }
        }
        public string ValueToolTip => CreateValueToolTipForEnum();
        public Visibility BacTypeVisibility => SelectedFileType == MoveFileTypes.BAC ? Visibility.Visible : Visibility.Collapsed;

        public bool ReplaceMode
        {
            get => _replaceMode;
            set
            {
                _replaceMode = value;
                NotifyPropertyChanged(nameof(ReplaceMode));
                UpdateUIElements();
            }
        }
        public bool NotMode { get; set; }
        public string CurrentLogMessage { get; set; }



        public FindAndReplace(MainWindow parent)
        {
            Owner = parent;
            mainWindow = parent;
            DataContext = this;
            InitializeComponent();
        }

        public RelayCommand DoneButtonCommand => new RelayCommand(FindOrReplace);
        private void FindOrReplace()
        {
            if (!ValidateInputs())
            {
                LogLocalMessage("Unable to parse input values.");
                return;
            }

            if (ReplaceMode && (string.IsNullOrWhiteSpace(ValueToFind) || string.IsNullOrWhiteSpace(ValueToReplace)))
            {
                LogLocalMessage("Input values are empty.");
                return;
            }

            if (Files.Instance.SelectedMove == null)
            {
                LogLocalMessage("Nothing selected in the Outliner.");
                return;
            }

            if (SelectedFileType == MoveFileTypes.BAC)
            {
                if (ReplaceMode)
                {
                    int numReplaced;
                    object valueToFind = null;
                    object valueToReplace = null;

                    ConvertToType(ValueToFind, SelectedValue.valueType, ref valueToFind);
                    ConvertToType(ValueToReplace, SelectedValue.valueType, ref valueToReplace);

                    var undos = Find.ReplaceBacValue(Files.Instance.SelectedMove.Files.BacFile.File.BacEntries, SelectedBacType, SelectedValue.valueName, valueToFind, valueToReplace, out numReplaced);

                    if (undos.Count > 0)
                        UndoManager.Instance.AddCompositeUndo(undos, "Replace All");

                    LogLocalMessage($"Replaced {numReplaced} values.");
                }
                else
                {
                    BAC_Entry bacEntry;
                    object bacType;
                    object valueToFind = null;

                    ConvertToType(ValueToFind, SelectedValue.valueType, ref valueToFind);

                    Find.FindBacValue(Files.Instance.SelectedItem.SelectedBacFile.File.BacEntries, SelectedBacType, SelectedValue.valueName, valueToFind, prevFoundItem, NotMode, out bacEntry, out bacType);

                    prevFoundItem = bacType;

                    if (bacEntry != null && bacType != null)
                    {
                        mainWindow.bacControlView.bacEntryDataGrid.SelectedItem = bacEntry;
                        mainWindow.bacControlView.bacEntryDataGrid.ScrollIntoView(bacEntry);
                        mainWindow.bacControlView.SetSelectedBacType(bacType as IBacType);

                        LogLocalMessage("Found a matching value.");
                    }
                    else
                    {
                        LogLocalMessage("Nothing found.");
                    }
                }
            }
            else if (SelectedFileType == MoveFileTypes.BCM)
            {
                BCM_File file = Files.Instance.SelectedItem?.SelectedBcmFile?.File;
                if (file == null)
                {
                    LogLocalMessage("No BCM file is selected.");
                    return;
                }

                if (ReplaceMode)
                {
                    object valueToFind = null;
                    object valueToReplace = null;

                    ConvertToType(ValueToFind, SelectedValue.valueType, ref valueToFind);
                    ConvertToType(ValueToReplace, SelectedValue.valueType, ref valueToReplace);

                    List<IUndoRedo> undos = ReplaceBcmValue(file, SelectedValue.valueName, valueToFind, valueToReplace, out int numReplaced);
                    if (undos.Count > 0)
                        UndoManager.Instance.AddCompositeUndo(undos, "BCM Replace All");

                    mainWindow.bcmTabView.RefreshAfterFindReplace(SelectedValue.valueName);
                    LogLocalMessage($"Replaced {numReplaced} values.");
                }
                else
                {
                    object valueToFind = null;
                    ConvertToType(ValueToFind, SelectedValue.valueType, ref valueToFind);

                    BCM_Entry entry = FindBcmValue(file, SelectedValue.valueName, valueToFind, prevFoundItem as BCM_Entry, NotMode);
                    prevFoundItem = entry;

                    if (entry != null)
                    {
                        mainWindow.mainTabControl.SelectedItem = mainWindow.stateTab;
                        mainWindow.bcmTabView.SelectEntry(entry);
                        LogLocalMessage("Found a matching value.");
                    }
                    else
                    {
                        LogLocalMessage("Nothing found.");
                    }
                }
            }
            else
            {
                LogLocalMessage("Undefined file type!");
                return;
            }

            UpdateUIElements();
            UndoManager.Instance.ForceEventCall();
        }








        public RelayCommand ExitCommand => new RelayCommand(Exit);
        private void Exit()
        {
            Close();
        }


        private bool ValidateInputs()
        {
            object ret1 = null;
            if (!ConvertToType(ValueToFind, SelectedValue.valueType, ref ret1) && !string.IsNullOrWhiteSpace(ValueToFind))
                return false;

            if (ReplaceMode)
            {
                object ret2 = null;
                if (!ConvertToType(ValueToReplace, SelectedValue.valueType, ref ret2) && !string.IsNullOrWhiteSpace(ValueToReplace))
                    return false;
            }

            return true;
        }

        private bool ConvertToType(string value, Type type, ref object result)
        {
            if (type == null) return false;

            value = value?.Trim() ?? string.Empty;

            if (type.IsString())
            {
                result = value;
                return true;
            }

            if (type.IsBool())
            {
                if (value.ToLower() != "true" && value.ToLower() != "false")
                    return false;

                result = value.ToLower() == "true";
                return true;
            }

            if (type.IsEnum())
            {
                try
                {
                    result = Enum.Parse(type, value, true);
                    return true;
                }
                catch
                {
                    if (!TryParseInteger(value, out long enumNumber))
                        return false;

                    result = Enum.ToObject(type, enumNumber);
                    return true;
                }
            }

            if (type.IsFloat())
            {
                float ret;
                if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out ret))
                    return false;

                result = ret;
            }
            else if (type.IsDouble())
            {
                double ret;
                if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out ret))
                    return false;

                result = ret;
            }
            else if (type.IsInt8())
            {
                if (!TryParseInteger(value, out long number) || number < byte.MinValue || number > byte.MaxValue)
                    return false;

                result = (byte)number;
            }
            else if (type.IsUInt8())
            {
                if (!TryParseInteger(value, out long number) || number < sbyte.MinValue || number > sbyte.MaxValue)
                    return false;

                result = (sbyte)number;
            }
            else if (type.IsInt16())
            {
                if (!TryParseInteger(value, out long number) || number < short.MinValue || number > short.MaxValue)
                    return false;

                result = (short)number;
            }
            else if (type.IsUInt16())
            {
                if (!TryParseInteger(value, out long number) || number < ushort.MinValue || number > ushort.MaxValue)
                    return false;

                result = (ushort)number;
            }
            else if (type.IsInt32())
            {
                if (!TryParseInteger(value, out long number) || number < int.MinValue || number > int.MaxValue)
                    return false;

                result = (int)number;
            }
            else if (type.IsUInt32())
            {
                if (!TryParseInteger(value, out long number) || number < uint.MinValue || number > uint.MaxValue)
                    return false;

                result = (uint)number;
            }
            else if (type.IsInt64())
            {
                if (!TryParseInteger(value, out long number))
                    return false;

                result = number;
            }
            else if (type == typeof(ulong))
            {
                if (!TryParseInteger(value, out long number) || number < 0)
                    return false;

                result = (ulong)number;
            }
            else
            {
                return false;
            }

            return true;
        }

        private static bool TryParseInteger(string value, out long result)
        {
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return long.TryParse(value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);

            return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        private string CreateValueToolTipForEnum()
        {
            if (SelectedValue.valueType == null) return null;
            if (!SelectedValue.valueType.IsEnum()) return null;

            StringBuilder str = new StringBuilder();
            str.Append("Possible values:\n");

            var enumValues = SelectedValue.valueType.GetEnumValues();
            var enumNames = SelectedValue.valueType.GetEnumNames();

            for(int i = 0; i < enumNames.Length; i++)
            {
                str.Append(enumNames[i]).AppendLine();
            }


            return str.ToString();
        }

        private void LogLocalMessage(string message)
        {
            CurrentLogMessage = message;
            NotifyPropertyChanged(nameof(CurrentLogMessage));
        }

        private void ResetState()
        {
            prevFoundItem = null;
            UpdateUIElements();
        }
    
        private void UpdateUIElements()
        {
            if (ReplaceMode)
            {
                button.Content = "Replace";
                replaceGrid.Visibility = Visibility.Visible;
                notCheckbox.Visibility = Visibility.Collapsed;
            }
            else
            {
                button.Content = (prevFoundItem != null) ? "Find Next" : "Find";
                replaceGrid.Visibility = Visibility.Hidden;
                notCheckbox.Visibility = Visibility.Visible;
            }
        }
    }
}
