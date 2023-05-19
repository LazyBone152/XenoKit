using System;
using System.ComponentModel;
using System.Windows.Controls;
using XenoKit.Engine;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Views
{
    /// <summary>
    /// Interaction logic for SettingsPanel.xaml
    /// </summary>
    public partial class SettingsPanel : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        public bool HideEmptryBacEntries
        {
            get
            {
                return SettingsManager.settings.XenoKit_HideEmptyBacEntries;
            }
            set
            {
                if (SettingsManager.settings.XenoKit_HideEmptyBacEntries != value)
                {
                    SettingsManager.settings.XenoKit_HideEmptyBacEntries = value;
                    SettingsManager.Instance.SaveSettings();
                }
            }
        }
        public bool RetainActionPosition
        {
            get => SceneManager.RetainActionMovement;
            set => SceneManager.RetainActionMovement = value;
        }
        public BacTypeSortMode BacTypeSortMode
        {
            get
            {
                return SettingsManager.settings.XenoKit_BacTypeSortModeEnum;
            }
            set
            {
                if (SettingsManager.settings.XenoKit_BacTypeSortModeEnum != value)
                {
                    SettingsManager.settings.XenoKit_BacTypeSortModeEnum = value;
                    SettingsManager.Instance.SaveSettings();
                }
            }
        }
        public bool RenderBoneNames
        {
            get
            {
                return SettingsManager.settings.XenoKit_RenderBoneNames;
            }
            set
            {
                if (SettingsManager.settings.XenoKit_RenderBoneNames != value)
                {
                    SettingsManager.settings.XenoKit_RenderBoneNames = value;
                    SettingsManager.Instance.SaveSettings();
                    NotifyPropertyChanged(nameof(RenderBoneNames));
                }
            }
        }
        public bool RenderBoneNamesMouseOver
        {
            get
            {
                return SettingsManager.settings.XenoKit_RenderBoneNamesMouseOverOnly;
            }
            set
            {
                if (SettingsManager.settings.XenoKit_RenderBoneNamesMouseOverOnly != value)
                {
                    SettingsManager.settings.XenoKit_RenderBoneNamesMouseOverOnly = value;
                    SettingsManager.Instance.SaveSettings();
                }
            }
        }
        public bool AutoResolvePasteReferences
        {
            get
            {
                return SettingsManager.settings.XenoKit_AutoResolvePasteReferences;
            }
            set
            {
                if (SettingsManager.settings.XenoKit_AutoResolvePasteReferences != value)
                {
                    SettingsManager.settings.XenoKit_AutoResolvePasteReferences = value;
                    SettingsManager.Instance.SaveSettings();
                }
            }
        }
        public bool FocusedBoneView
        {
            get
            {
                return SettingsManager.settings.XenoKit_HideLessImportantBones;
            }
            set
            {
                if (SettingsManager.settings.XenoKit_HideLessImportantBones != value)
                {
                    SettingsManager.settings.XenoKit_HideLessImportantBones = value;
                    SettingsManager.Instance.SaveSettings();
                }
            }
        }

        public SettingsPanel()
        {
            InitializeComponent();
            DataContext = this;
            SettingsManager.SettingsReloaded += SettingsManager_SettingsReloaded;
        }

        private void SettingsManager_SettingsReloaded(object sender, EventArgs e)
        {
            NotifyPropertyChanged(nameof(HideEmptryBacEntries));
            NotifyPropertyChanged(nameof(BacTypeSortMode));
            NotifyPropertyChanged(nameof(RenderBoneNames));
            NotifyPropertyChanged(nameof(RenderBoneNamesMouseOver));
            NotifyPropertyChanged(nameof(AutoResolvePasteReferences));
            NotifyPropertyChanged(nameof(FocusedBoneView));
        }
    
    
    }
}
