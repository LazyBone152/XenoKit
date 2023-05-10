using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            { MoveFileTypes.BAC , "BAC" }
        };

        public Dictionary<Type, string> BacTypes { get; private set; } = new Dictionary<Type, string>()
        {
            { typeof(BAC_Type0) , "Animation" },
            { typeof(BAC_Type1) , "Hitbox" },
            { typeof(BAC_Type2) , "Movement" },
            { typeof(BAC_Type3) , "Invulnerability" },
            { typeof(BAC_Type4) , "TimeScale" },
            { typeof(BAC_Type5) , "Tracking" },
            { typeof(BAC_Type6) , "ChargeControl" },
            { typeof(BAC_Type7) , "BcmCallback" },
            { typeof(BAC_Type8) , "Effect" },
            { typeof(BAC_Type9) , "Projectile" },
            { typeof(BAC_Type10) , "Camera" },
            { typeof(BAC_Type11) , "Sound" },
            { typeof(BAC_Type12) , "TargetingAssistance" },
            { typeof(BAC_Type13) , "BcsPartSetInvisibility" },
            { typeof(BAC_Type14) , "BoneModification" },
            { typeof(BAC_Type15) , "Functions" },
            { typeof(BAC_Type16) , "ScreenEffect" },
            { typeof(BAC_Type17) , "ThrowHandler" },
            { typeof(BAC_Type18) , "PhysicsObject" },
            { typeof(BAC_Type19) , "Aura" },
            { typeof(BAC_Type20) , "HomingMovement" },
            { typeof(BAC_Type21) , "EyeMovement" },
            { typeof(BAC_Type22) , "BAC_Type22" },
            { typeof(BAC_Type23) , "TransparencyEffect" },
            { typeof(BAC_Type24) , "DualSkillHandler" },
            { typeof(BAC_Type25) , "ExtendedChainAttack" },
            { typeof(BAC_Type26) , "ExtendedCameraControl" },
            { typeof(BAC_Type27) , "EffectPropertyControl" },
            { typeof(BAC_Type28) , "BAC_Type28" },
            { typeof(BAC_Type29) , "BAC_Type29" },
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
                    ResetState();
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
                //add different logic here when other file types are added
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

                    Find.FindBacValue(Files.Instance.SelectedMove.Files.BacFile.File.BacEntries, SelectedBacType, SelectedValue.valueName, valueToFind, prevFoundItem, NotMode, out bacEntry, out bacType);

                    prevFoundItem = bacType;

                    if (bacEntry != null && bacType != null)
                    {
                        mainWindow.bacControlView.bacEntryDataGrid.SelectedItem = bacEntry;
                        mainWindow.bacControlView.bacTypeDataGrid.SelectedItem = bacType;
                        mainWindow.bacControlView.bacEntryDataGrid.ScrollIntoView(bacEntry);
                        mainWindow.bacControlView.bacTypeDataGrid.ScrollIntoView(bacType);

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

            if (type.IsInt8())
            {
                byte ret;
                if (!byte.TryParse(value, out ret))
                    return false;

                result = ret;
            }
            else if (type.IsUInt8())
            {
                sbyte ret;
                if (!sbyte.TryParse(value, out ret))
                    return false;

                result = ret;
            }
            else if (type.IsInt16())
            {
                short ret;
                if (!short.TryParse(value, out ret))
                    return false;

                result = ret;
            }
            else if (type.IsUInt16())
            {
                ushort ret;
                if (!ushort.TryParse(value, out ret))
                    return false;

                result = ret;
            }
            else if (type.IsInt32())
            {
                int ret;
                if (!int.TryParse(value, out ret))
                    return false;

                result = ret;
            }
            else if (type.IsUInt32())
            {
                uint ret;
                if (!uint.TryParse(value, out ret))
                    return false;

                result = ret;
            }
            else if (type.IsInt64())
            {
                long ret;
                if (!long.TryParse(value, out ret))
                    return false;

                result = ret;
            }
            else if (type.IsInt64())
            {
                ulong ret;
                if (!ulong.TryParse(value, out ret))
                    return false;

                result = ret;
            }
            else if (type.IsFloat())
            {
                float ret;
                if (!float.TryParse(value, out ret))
                    return false;

                result = ret;
            }
            else if (type.IsDouble())
            {
                double ret;
                if (!double.TryParse(value, out ret))
                    return false;

                result = ret;
            }
            else if (type.IsBool())
            {
                if (value.ToLower() != "true" && value.ToLower() != "false")
                    return false;

                result = (value.ToLower() == "true") ? true : false;
            }
            else if (type.IsString())
            {
                result = value;
            }
            else if (type.IsEnum())
            {
                object ret = null;

                if(type == typeof(BAC_Type0.EanTypeEnum))
                {
                    if (!ParseEnum<BAC_Type0.EanTypeEnum>(value, type, ref ret)) return false;
                }
                if (type == typeof(BAC_Type10.EanTypeEnum))
                {
                    if (!ParseEnum<BAC_Type10.EanTypeEnum>(value, type, ref ret)) return false;
                }
                if (type == typeof(BoneLinks))
                {
                    if (!ParseEnum<BoneLinks>(value, type, ref ret)) return false;
                }
                if (type == typeof(AcbType))
                {
                    if (!ParseEnum<AcbType>(value, type, ref ret)) return false;
                }
                if (type == typeof(BcsPartId))
                {
                    if (!ParseEnum<BcsPartId>(value, type, ref ret)) return false;
                }
                if (type == typeof(AuraType))
                {
                    if (!ParseEnum<AuraType>(value, type, ref ret)) return false;
                }
                if (type == typeof(TargetingAxis))
                {
                    if (!ParseEnum<TargetingAxis>(value, type, ref ret)) return false;
                }
                if (type == typeof(BAC_Type8.EepkTypeEnum))
                {
                    if (!ParseEnum<BAC_Type8.EepkTypeEnum>(value, type, ref ret)) return false;
                }
                if (type == typeof(BAC_Type20.HomingType))
                {
                    if (!ParseEnum<BAC_Type20.HomingType>(value, type, ref ret)) return false;
                }
                if (type == typeof(BAC_Type1.BoundingBoxTypeEnum))
                {
                    if (!ParseEnum<BAC_Type1.BoundingBoxTypeEnum>(value, type, ref ret)) return false;
                }

                result = ret;
            }

            return true;
        }

        private bool ParseEnum<T>(string value, Type type, ref object result) where T : struct, IConvertible
        {
            T ret;
            if (!Enum.TryParse(value, out ret))
                return false;

            result = ret;

            return true;
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
