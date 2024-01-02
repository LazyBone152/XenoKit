using GalaSoft.MvvmLight;
using System;
using System.Windows;
using XenoKit.Engine;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BAC
{
    public class BACType15ViewModel : ObservableObject, IDisposable
    {

        private readonly BAC_Type15 bacType;

        public int FunctionType
        {
            get
            {
                return bacType.FunctionType;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type15>(nameof(bacType.FunctionType), bacType, bacType.FunctionType, value, "FunctionType"), UndoGroup.Action, "Function", bacType);
                bacType.FunctionType = value;
                RaisePropertyChanged(() => FunctionType);
                bacType.RefreshType();
                RefreshUI();
                UndoManager.Instance.ForceEventCall(UndoGroup.Action, "Function", bacType);
            }
        }
        public float Param1
        {
            get
            {
                return bacType.Param1;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type15>(nameof(bacType.Param1), bacType, bacType.Param1, value, "FunctionParam1"));
                bacType.Param1 = value;
                RaisePropertyChanged(() => Param1);
            }
        }
        public float Param2
        {
            get
            {
                return bacType.Param2;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type15>(nameof(bacType.Param2), bacType, bacType.Param2, value, "FunctionParam2"));
                bacType.Param2 = value;
                RaisePropertyChanged(() => Param2);
            }
        }
        public float Param3
        {
            get
            {
                return bacType.Param3;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type15>(nameof(bacType.Param3), bacType, bacType.Param3, value, "FunctionParam3"));
                bacType.Param3 = value;
                RaisePropertyChanged(() => Param3);
            }
        }
        public float Param4
        {
            get
            {
                return bacType.Param4;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type15>(nameof(bacType.Param4), bacType, bacType.Param4, value, "FunctionParam4"));
                bacType.Param4 = value;
                RaisePropertyChanged(() => Param4);
            }
        }
        public float Param5
        {
            get
            {
                return bacType.Param5;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type15>(nameof(bacType.Param5), bacType, bacType.Param5, value, "FunctionParam5"));
                bacType.Param5 = value;
                RaisePropertyChanged(() => Param5);
            }
        }

        //UI
        public bool IsParam1Enabled => IsParamEnabled(1);
        public bool IsParam2Enabled => IsParamEnabled(2);
        public bool IsParam3Enabled => IsParamEnabled(3);
        public bool IsParam4Enabled => IsParamEnabled(4);
        public bool IsParam5Enabled => IsParamEnabled(5);

        public string Param1Name => GetParam1Name();
        public string Param2Name => GetParam2Name();
        public string Param3Name => GetParam3Name();

        public string Param1ToolTip => GetParam1ToolTip();
        public string Param2ToolTip => GetParam2ToolTip();

        //Visibility
        public Visibility Params2Visibility => (FunctionType == 0x25 || FunctionType == 0x26 || FunctionType == 0x4E) ? Visibility.Collapsed : Visibility.Visible;
        public Visibility Params2SkillIdVisibility => (FunctionType == 0x25 || FunctionType == 0x26 || FunctionType == 0x4E) ? Visibility.Visible : Visibility.Collapsed;

        public BACType15ViewModel(BAC_Type15 _bacType)
        {
            bacType = _bacType;
            bacType.PropertyChanged += BacType_PropertyChanged;

            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
        }

        public void Dispose()
        {
            UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
            bacType.PropertyChanged -= BacType_PropertyChanged;
        }

        private void Instance_UndoOrRedoCalled(object sender, System.EventArgs e)
        {
            UpdateProperties();
        }

        private void BacType_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(e.PropertyName);
        }

        private void UpdateProperties()
        {
            //Needed for updating properties when undo/redo is called
            RaisePropertyChanged(() => FunctionType);
            RaisePropertyChanged(() => Param1);
            RaisePropertyChanged(() => Param2);
            RaisePropertyChanged(() => Param3);
            RaisePropertyChanged(() => Param4);
            RaisePropertyChanged(() => Param5);
            RefreshUI();
        }

        private void UpdateBacPlayer()
        {
            SceneManager.InvokeBacDataChangedEvent();
        }
    
        private bool IsParamEnabled(int num)
        {
            int numParams;
            if (!Xv2CoreLib.ValuesDictionary.BAC.BacFunctionParamCount.TryGetValue(FunctionType, out numParams))
                return true;

            return numParams >= num;
        }

        private string GetParam1Name()
        {
            string name;
            if (!Xv2CoreLib.ValuesDictionary.BAC.BacFunctionParam1Names.TryGetValue(FunctionType, out name))
                return "Parameter 1";

            return name;
        }

        private string GetParam2Name()
        {
            string name;
            if (!Xv2CoreLib.ValuesDictionary.BAC.BacFunctionParam2Names.TryGetValue(FunctionType, out name))
                return "Parameter 2";

            return name;
        }

        private string GetParam3Name()
        {
            string name;
            if (!Xv2CoreLib.ValuesDictionary.BAC.BacFunctionParam3Names.TryGetValue(FunctionType, out name))
                return "Parameter 3";

            return name;
        }
    
        private string GetParam1ToolTip()
        {
            string tooltip;
            if (!Xv2CoreLib.ValuesDictionary.BAC.BacFunctionParam1ToolTips.TryGetValue(FunctionType, out tooltip))
                return null;

            return tooltip;
        }

        private string GetParam2ToolTip()
        {
            string tooltip;
            if (!Xv2CoreLib.ValuesDictionary.BAC.BacFunctionParam2ToolTips.TryGetValue(FunctionType, out tooltip))
                return null;

            return tooltip;
        }

        private void RefreshUI()
        {
            RaisePropertyChanged(() => IsParam1Enabled);
            RaisePropertyChanged(() => IsParam2Enabled);
            RaisePropertyChanged(() => IsParam3Enabled);
            RaisePropertyChanged(() => IsParam4Enabled);
            RaisePropertyChanged(() => IsParam5Enabled);
            RaisePropertyChanged(() => Param1Name);
            RaisePropertyChanged(() => Param2Name);
            RaisePropertyChanged(() => Param3Name);
            RaisePropertyChanged(() => Param1ToolTip);
            RaisePropertyChanged(() => Param2ToolTip);
            RaisePropertyChanged(() => Params2Visibility);
            RaisePropertyChanged(() => Params2SkillIdVisibility);
        }
    }
}
