using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using XenoKit.Windows;
using Xv2CoreLib;
using Xv2CoreLib.ACB;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BCS;
using Xv2CoreLib.BDM;
using Xv2CoreLib.BSA;
using Xv2CoreLib.CUS;
using Xv2CoreLib.EAN;
using Xv2CoreLib.EffectContainer;
using xv2 = Xv2CoreLib.Xenoverse2;
using file = Xv2CoreLib.FileManager;
using XenoKit.Engine;
using GalaSoft.MvvmLight.CommandWpf;
using System.Runtime.ExceptionServices;
using Xv2CoreLib.Resource;
using Xv2CoreLib.ValuesDictionary;
using System.Windows;
using Xv2CoreLib.Resource.App;
using Xv2CoreLib.SAV;
using Xv2CoreLib.Resource.UndoRedo;
using Xv2CoreLib.SPM;
using XenoKit.Engine.Stage;
using ControlzEx.Standard;

namespace XenoKit.Editor
{
    public sealed partial class Files : INotifyPropertyChanged
    {
        #region INotifyPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region Singleton
        private static Lazy<Files> instance = new Lazy<Files>(() => new Files());
        public static Files Instance => instance.Value;

        private Files()
        {
            SelectedItemChanged += SelectedItemOrTabChanged;
        }
        #endregion

        private MainWindow window = null;
        private readonly Dictionary<string, EffectContainerFile> stageEepkCache = new Dictionary<string, EffectContainerFile>();
        private readonly HashSet<string> missingStageEepks = new HashSet<string>();

        public AsyncObservableCollection<OutlinerItem> OutlinerItems { get; set; } = new AsyncObservableCollection<OutlinerItem>();

        private OutlinerItem _selectedItem = null;
        public OutlinerItem SelectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                _selectedItem = value;
                NotifyPropertyChanged(nameof(SelectedItem));
                NotifyPropertyChanged(nameof(SelectedMove));
                NotifyPropertyChanged(nameof(SelectedStage));
                SelectedItemChanged?.Invoke(value, EventArgs.Empty);
            }
        }
        public Move SelectedMove
        {
            get => SelectedItem?.GetMove();
        }
        public Xv2Stage SelectedStage
        {
            get => SelectedItem?.Stage;
        }

        public string SaveContextMenuString
        {
            get
            {
                if(SceneManager.CurrentDynamicTab != DynamicTabs.None)
                {
                    DynamicTab tab = TabManager.GetSelectedDynamicTab();
                    return tab.Context.CanSave() ? $"_Save File ({tab.Context.GetSaveContextFileName()})" : "Save File (N/A)";
                }
                else if (SceneManager.CurrentSceneState == EditorTabs.FPF)
                {
                    string fpfFileName = window?.fpfTabView?.GetSaveContextFileName();
                    return fpfFileName == null ? "Save File (N/A)" : $"_Save File ({fpfFileName})";
                }
                else
                {
                    var str = SelectedItem?.GetSaveContextFileName();

                    return str == null ? $"Save File (N/A)" : $"_Save File ({str})";
                }
            }
        }
        public string SaveCurrentMenuString
        {
            get
            {
                return string.IsNullOrWhiteSpace(SelectedItem?.DisplayName) ? $"Save (N/A)" : $"_Save ({SelectedItem.DisplayName})";
            }
        }

        public static event EventHandler SelectedItemChanged;


        /// <summary>
        /// Load game CPKs and name list files. A progress bar will appear on window as they load.
        /// </summary>
        public async void Initialize(MainWindow window)
        {
            this.window = window;

            var controller = await window.ShowProgressAsync("Initializing", "Reading game files...", false, DialogSettings.Default);
            controller.SetIndeterminate();

            AddOutlinerItem(new OutlinerItem(true, OutlinerItem.OutlinerItemType.Inspector, false));

            try
            {
                await Task.Run(() =>
                {
                    xv2.Instance.loadCharacters = true;
                    xv2.Instance.loadSkills = true;
                    xv2.Instance.loadCmn = true;
                    xv2.Instance.loadStage = true;
                    xv2.Instance.Init();
                });


                if (GetCmnMove() == null && !SettingsManager.settings.XenoKit_DelayLoadingCMN)
                {
                    //By not awaiting it, we can just let the cmn files load in the background the not block the start up of XenoKit.
                    //It will appear in the outliner once it is finished
                    Task.Run(() =>
                    {
                        LoadCmnFiles();
                    });
                    //await AsyncLoadCmnFiles(controller);
                }
            }
            finally
            {
                await controller.CloseAsync();

                //Go to viewer tab and select viewer mode
                window.mainTabControl.SelectedIndex = (int)MainEditorTabs.Inspector;
                SelectedItem = OutlinerItems[0];
            }
        }


        private async Task AsyncLoadCmnFiles(ProgressDialogController controller)
        {
            controller.SetMessage("Loading common files...");

            LoadCmnFiles();
        }

        #region RightClickMenuCommands








        #endregion


        public void SelectedItemOrTabChanged(object sender, EventArgs e)
        {
            NotifyPropertyChanged(nameof(SaveContextMenuString));
            NotifyPropertyChanged(nameof(SaveCurrentMenuString));
        }

        #region Load













        #endregion

        #region Save







        #endregion

        #region GetHelpers

        /// <summary>
        /// Get a read-only list of all characters in the current scene.
        /// </summary>
        /// <returns></returns>

        /// <summary>
        /// Returns the file instance associated with path, if it has been previously loaded.
        /// </summary>

        //File Get
        /// <summary>
        /// Get the Ean File declared by a BAC_Type0 (Animation).
        /// </summary>
        /// <param name="eanType">The ean type.</param>
        /// <param name="move">The skill object. If eanType is for a skill, then the ean from this will be returned.</param>
        /// <param name="character">The chracter object. If eanType is for a character, then the ean from this will be returned. Also used in getting the correct character unique skill EAN file (if any).</param>
        /// <param name="logErrors">Log errors?</param>
        /// <param name="charaUnique">Use character unique EAN if present.</param>

        /// <summary>
        /// Get the cam.ean file declared by a BAC_Type10 (Camera).
        /// </summary>
        /// <param name="eanType">The ean type.</param>
        /// <param name="move">The skill object. If eanType is for a skill, then the ean from this will be returned.</param>
        /// <param name="character">The chracter object. If eanType is for a character, then the ean from this will be returned. Also used in getting the correct character unique skill EAN file (if any).</param>
        /// <param name="logErrors">Log errors?</param>
        /// <param name="charaUnique">Use character unique EAN if present.</param>






        #endregion


    }
}
