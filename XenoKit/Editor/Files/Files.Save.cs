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
        public async void SaveItem(OutlinerItem item, bool log = true)
        {
            if (item.ReadOnly)
            {
                Log.Add($"{item.DisplayName} ({item.Type}) is read-only, cannot save!", LogType.Error);
                return;
            }

            if (item.IsManualLoaded)
            {
                item.ManualFiles.Save();
                return;
            }

            //Validation
            if (!item.SaveValidate(window))
            {
                Log.Add($"{item.DisplayName} ({item.Type}) save failed due to validation errors", LogType.Error);
                return;
            }


            var progressBarController = await window.ShowProgressAsync("Saving", "Save in progress...", false, DialogSettings.Default);
            progressBarController.SetIndeterminate();

#if !DEBUG
            try
#endif
            {
                await Task.Run(() =>
                {
                    switch (item.Type)
                    {
                        case OutlinerItem.OutlinerItemType.Character:
                            SaveCharacter(item);
                            break;
                        case OutlinerItem.OutlinerItemType.Moveset:
                            SaveMoveset(item);
                            break;
                        case OutlinerItem.OutlinerItemType.Skill:
                            SaveSkill(item);
                            break;
                        case OutlinerItem.OutlinerItemType.CMN:
                            SaveCMN(item.move);
                            break;
                        case OutlinerItem.OutlinerItemType.CaC:
                            SaveCac(item);
                            break;
                        case OutlinerItem.OutlinerItemType.Inspector:
                            Inspector.InspectorMode.Instance.SaveFiles();
                            break;
                    }
                });

            }
#if !DEBUG
            catch (Exception ex)
            {
                Log.Add($"Save Error: {ex.Message}", LogType.Error);
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
            finally
#endif
            {
                if (log)
                    Log.Add($"{item.DisplayName} ({item.Type}) saved!", LogType.Info);

                await progressBarController.CloseAsync();
            }

        }

        public void SaveAll()
        {
            int count = 0;

            foreach (var file in OutlinerItems)
            {
                if (!file.ReadOnly)
                {
                    count++;
                    SaveItem(file, false);
                }
            }

            Log.Add($"Saved {count} items!", LogType.Info);
        }

        private void SaveCMN(Move cmn)
        {
            //Convert IBacTypes back into individual lists
            foreach (var bac in cmn.Files.BacFiles)
            {
                bac.File.SaveIBacTypes();
            }

            cmn.Files.BsaFile?.File?.SaveIBsaTypes();

            cmn.ConvertToXv2Skill().SaveMoveFiles();
        }

        private void SaveSkill(OutlinerItem item)
        {
            //Convert IBacTypes/IBsaTypes back into individual lists
            foreach(var bac in item.GetMove().Files.BacFiles)
            {
                bac.File.SaveIBacTypes();
            }

            item.GetMove().Files.BsaFile?.File?.SaveIBsaTypes();

            if (item.IsManualLoaded)
            {
                item.move.ConvertToXv2Skill().SaveMoveFiles();
            }
            else
            {
                xv2.Instance.SaveSkill(item.move.ConvertToXv2Skill());
            }
        }

        private void SaveCharacter(OutlinerItem item)
        {
            //Convert IBacTypes back into individual lists
            item.GetMove().Files.BacFile?.File?.SaveIBacTypes();

            if (item.IsManualLoaded || item.character.CharacterData.IsCaC)
            {
                item.character.ConvertToXv2Character().SaveFiles();
            }
            else
            {
                xv2.Instance.SaveCharacter(item.character.ConvertToXv2Character(), false);
            }
        }

        private void SaveMoveset(OutlinerItem item)
        {
            //Convert IBacTypes back into individual lists
            item.GetMove().Files.BacFile?.File?.SaveIBacTypes();

            if (item.IsManualLoaded)
            {
                item.move.ConvertToXv2Character().SaveFiles();
            }
            else
            {
                xv2.Instance.SaveCharacter(item.move.ConvertToXv2Character(), true);
            }
        }

        private void SaveCac(OutlinerItem item)
        {
            if (!File.Exists(SettingsManager.settings.SaveFile))
            {
                MessageBox.Show("A save file must be set in the settings to use this feature.", "No Save File", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //Reload the save file, because XenoKit doesn't keep it in memory
            SAV_File savFile = SAV_File.Load(SettingsManager.settings.SaveFile, false);

            //Only the appearence settings are saved back to the save file
            savFile.Characters[item.CustomAvatar.CaCIndex].Appearence = item.CustomAvatar.CaC.Appearence;
            savFile.Characters[item.CustomAvatar.CaCIndex].Presets = item.CustomAvatar.CaC.Presets;

            savFile.Save(SettingsManager.settings.SaveFile, true);
        }

    }
}
