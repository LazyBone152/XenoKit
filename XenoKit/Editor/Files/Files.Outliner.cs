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
        public void AddOutlinerItem(OutlinerItem outlinerItem)
        {
            lock (OutlinerItems)
            {
                OutlinerItems.Add(outlinerItem);
            }
        }

        public RelayCommand SaveSelectedItemCommand => new RelayCommand(SaveSelectedItem, CanSave);

        private void SaveSelectedItem()
        {
            SaveItem(_selectedItem);
        }

        public RelayCommand SaveContextFileCommand => new RelayCommand(SaveContextFile, CanSaveContextFile);

        private void SaveContextFile()
        {
            if (SceneManager.CurrentDynamicTab != DynamicTabs.None)
            {
                TabManager.GetSelectedDynamicTab().Context.Save();
            }
            else if (SceneManager.CurrentSceneState == EditorTabs.FPF)
            {
                window?.fpfTabView?.SaveContextFile();
            }
            else
            {
                SelectedItem.SaveContextFile();
            }
        }

        public async void RemoveSelectedItem(IList<OutlinerItem> items)
        {
            int count = items.Where(x => x.CanDelete).Count();

            string message = count == 1 ? $"Do you want to remove \"{SelectedItem.DisplayName}\"? It will be removed from the outliner and any edits will be lost if not saved." :
                $"{count} items will be removed from the outliner and any edits made to them will be lost if not saved.";

            MessageDialogResult result = await window.ShowMessageAsync(items.Count > 1 ? "Remove Items" : "Remove Item", message, MessageDialogStyle.AffirmativeAndNegative, DialogSettings.Default);

            if (result == MessageDialogResult.Affirmative)
            {
                SelectedItem = null;

                foreach (OutlinerItem item in items.Where(x => x.CanDelete))
                {
                    if (item.Type == OutlinerItem.OutlinerItemType.Stage && Viewport.Instance.CurrentStage == item.Stage)
                    {
                        Viewport.Instance.SetActiveStage(null);
                    }

                    SceneManager.UnsetActor(item.character);

                    lock(OutlinerItems)
                        OutlinerItems.Remove(item);

                    TabManager.RemoveTabsForParent(item);
                }
            }
        }

        public RelayCommand ReloadSelectedItemCommand => new RelayCommand(ReloadSelectedItem, CanReload);

        private async void ReloadSelectedItem()
        {
            FileManager.Instance.ForceReloadFiles = true;

            try
            {
                UndoManager.Instance.Clear();
                int index = OutlinerItems.IndexOf(_selectedItem);
                string name = _selectedItem.DisplayName;

                switch (_selectedItem.Type)
                {
                    case OutlinerItem.OutlinerItemType.Skill:
                        await AsyncLoadSkill(_selectedItem.move.CusEntry.ID1, _selectedItem.move.SkillType, _selectedItem.OnlyLoadFromCPK, index);
                        break;
                    case OutlinerItem.OutlinerItemType.Character:
                        int actorSlot = SceneManager.UnsetActor(_selectedItem.character);
                        Actor actor = await AsyncLoadCharacter(_selectedItem.character.CharacterData.CmsEntry.ID, _selectedItem.character.PartSet.ID, _selectedItem.ReadOnly, index, _selectedItem.OnlyLoadFromCPK);

                        //Set new actor as actor if the reloaded character was previously an actor
                        if(actorSlot != -1)
                            SceneManager.SetActor(actor, actorSlot);
                        break;
                    case OutlinerItem.OutlinerItemType.CMN:
                        await Task.Run(LoadCmnFiles);
                        break;
                }

                Log.Add($"\"{name}\" reloaded!", LogType.Info);
            }
            finally
            {
                FileManager.Instance.ForceReloadFiles = false;
            }
        }

        private bool CanSaveContextFile()
        {
            if (SceneManager.CurrentDynamicTab != DynamicTabs.None)
            {
                DynamicTab tab = TabManager.GetSelectedDynamicTab();
                return tab.Context.CanSave();
            }
            else if (SceneManager.CurrentSceneState == EditorTabs.FPF)
            {
                return window?.fpfTabView?.CanSaveContextFile() == true;
            }
            else
            {
                return SelectedItem?.GetSaveContextFileName() != null;
            }
        }

        private bool CanSave()
        {
            return _selectedItem?.ReadOnly == false;
        }

        private bool CanReload()
        {
            if (_selectedItem == null) return false;
            return _selectedItem.Type == OutlinerItem.OutlinerItemType.Skill || _selectedItem.Type == OutlinerItem.OutlinerItemType.Character;
        }

        private async void AddOutlinerItem(OutlinerItem item, int replaceItemIndex = -1, bool selectItem = false)
        {
            if(replaceItemIndex != -1)
            {
                OutlinerItems[replaceItemIndex] = item;
                SelectOutlinerItem(item);
            }
            else
            {
                bool itemAdded = false;
                bool replacedExistingItem = false;

                lock(OutlinerItems)
                {
                    OutlinerItem existingItem = OutlinerItems.FirstOrDefault(x => x.ID == item.ID && !x.IsManualLoaded);

                    if (existingItem != null && !item.IsManualLoaded)
                    {
                        //Special case: we can replace the existing moveset item with the character here, since the character contains everything the moveset has.
                        if (existingItem.Type == OutlinerItem.OutlinerItemType.Moveset && item.Type == OutlinerItem.OutlinerItemType.Character)
                        {
                            OutlinerItems[OutlinerItems.IndexOf(existingItem)] = item;

                            Log.Add($"Replaced the moveset {existingItem.DisplayName} with the character {item.DisplayName}.");
                            itemAdded = true;
                            replacedExistingItem = true;
                        }
                        else
                        {
                            //Show an error message to the user and quit
                            MessageBox.Show($"This {item.DisplayType.ToLower()} is already loaded.", "Already Exists", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    if (!replacedExistingItem)
                    {
                        OutlinerItems.Add(item);
                        itemAdded = true;
                    }
                }

                if (selectItem && itemAdded)
                    SelectOutlinerItem(item);
            }
        }

        private void SelectOutlinerItem(OutlinerItem item)
        {
            if (System.Windows.Application.Current?.Dispatcher?.CheckAccess() != false)
            {
                SelectedItem = item;
                return;
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() => SelectedItem = item);
        }

    }
}
