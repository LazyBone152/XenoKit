using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using XenoKit.Engine.Animation;
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
using Xv2CoreLib.ESK;
using xv2 = Xv2CoreLib.Xenoverse2;
using file = Xv2CoreLib.FileManager;
using xv2Utils = Xv2CoreLib.Utils;
using XenoKit.Engine;
using GalaSoft.MvvmLight.CommandWpf;
using System.Runtime.ExceptionServices;
using Application = System.Windows.Application;
using Xv2CoreLib.Resource;
using Xv2CoreLib.ValuesDictionary;
using XenoKit.Editor.Data;
using System.Windows;
using Xv2CoreLib.Resource.App;
using Xv2CoreLib.SAV;
using Xv2CoreLib.Resource.UndoRedo;
using System.Threading;
using System.Globalization;

namespace XenoKit.Editor
{
    public sealed class Files : INotifyPropertyChanged
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

        private MetroWindow window = null;

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
                SelectedItemChanged?.Invoke(value, EventArgs.Empty);
            }
        }
        public Move SelectedMove
        {
            get
            {
                return SelectedItem?.GetMove();
            }
        }

        public string SaveContextMenuString
        {
            get
            {
                var str = SelectedItem?.GetSaveContextFileName();

                return str == null ? $"Save File (N/A)" : $"_Save File ({str})";
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

            OutlinerItems.Add(new OutlinerItem(true, OutlinerItem.OutlinerItemType.Inspector, false));

            try
            {
                await Task.Run(() =>
                {
                    xv2.Instance.loadCharacters = true;
                    xv2.Instance.loadSkills = true;
                    xv2.Instance.loadCmn = true;
                    xv2.Instance.Init();
                });


                if (GetCmnMove() == null && !SettingsManager.settings.XenoKit_DelayLoadingCMN)
                {
                    await AsyncLoadCmnFiles(controller);
                }
            }
            catch (Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
            finally
            {
                await controller.CloseAsync();

                //Go to viewer tab and select viewer mode
                window.mainTabControl.SelectedIndex = (int)EditorTabs.Inspector;
                SelectedItem = OutlinerItems[0];
            }
        }

        private async Task AsyncLoadCmnFiles(ProgressDialogController controller)
        {
            controller.SetMessage("Loading common files...");
            await Task.Run(LoadCmnFiles);
        }

        #region RightClickMenuCommands
        public RelayCommand SaveSelectedItemCommand => new RelayCommand(SaveSelectedItem, CanSave);
        private void SaveSelectedItem()
        {
            SaveItem(_selectedItem);
        }

        public RelayCommand SaveContextFileCommand => new RelayCommand(SaveContextFile, () => SelectedItem?.GetSaveContextFileName() != null);
        private void SaveContextFile()
        {
            SelectedItem.SaveContextFile();
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
                    if (SceneManager.MainGameInstance.ActiveStage == item.ManualFiles)
                    {
                        SceneManager.MainGameInstance.ActiveStage = null;
                    }

                    SceneManager.UnsetActor(item.character);
                    OutlinerItems.Remove(item);
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
                        Actor actor = await AsyncLoadCharacter(_selectedItem.character.CharacterData.CmsEntry.ID, _selectedItem.character.PartSet.ID, _selectedItem.ReadOnly, index);

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


        private bool CanSave()
        {
            return _selectedItem?.ReadOnly == false;
        }

        private bool CanReload()
        {
            if (_selectedItem == null) return false;
            return _selectedItem.Type == OutlinerItem.OutlinerItemType.Skill || _selectedItem.Type == OutlinerItem.OutlinerItemType.Character;
        }
        #endregion

        private async void AddOutlinerItem(OutlinerItem item, int replaceItemIndex = -1)
        {
            if(replaceItemIndex != -1)
            {
                OutlinerItems[replaceItemIndex] = item;
            }
            else
            {
                OutlinerItem existingItem = OutlinerItems.FirstOrDefault(x => x.ID == item.ID && !x.IsManualLoaded);

                if (existingItem != null && !item.IsManualLoaded)
                {
                    //Special case: we can replace the existing moveset item with the character here, since the character contains everything the moveset has.
                    if (existingItem.Type == OutlinerItem.OutlinerItemType.Moveset && item.Type == OutlinerItem.OutlinerItemType.Character)
                    {
                        OutlinerItems[OutlinerItems.IndexOf(existingItem)] = item;

                        Log.Add($"Replaced the moveset {existingItem.DisplayName} with the character {item.DisplayName}.");
                        return;
                    }

                    //Show an error message to the user and quit
                    MessageBox.Show($"This {item.DisplayType.ToLower()} is already loaded.", "Already Exists", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                OutlinerItems.Add(item);
            }
        }

        public void SelectedItemOrTabChanged(object sender, EventArgs e)
        {
            NotifyPropertyChanged(nameof(SaveContextMenuString));
            NotifyPropertyChanged(nameof(SaveCurrentMenuString));
        }

        #region Load
        public void ProcessFileDrop(string[] paths)
        {
            bool error = false;
            foreach (string drop in paths)
            {
                switch (Path.GetExtension(drop))
                {
                    case ".ean":
                    case ".acb":
                    case ".eepk":
                    case ".vfxpackage":
                        ManualLoad(drop);
                        break;
                    case ".nsk":
                        ManualLoad(drop);
                        return;
                    default:
                        if (!error)
                            MessageBox.Show($"The filetype of \"{drop}\" is not supported.", "File Drop", MessageBoxButton.OK, MessageBoxImage.Error);
                        error = true;
                        break;
                }
            }
        }

        public void ManualLoad(string filePath)
        {
            OutlinerItems.Add(new OutlinerItem(filePath));
        }

        public void ManualLoad(string[] filePaths)
        {
            OutlinerItems.Add(new OutlinerItem(filePaths));
        }

        public async void AsyncLoadSkill(CUS_File.SkillType skillType)
        {
            List<Xv2Item> skills = xv2.Instance.GetSkillList(skillType);
            EntitySelector selector = new EntitySelector(skills, skillType.ToString());
            selector.SetBooleanParameter("Only Load From CPK", "Ignore loose files and load directly from CPK.");
            selector.ShowDialog();

            if (selector.SelectedItem != null)
                await AsyncLoadSkill(selector.SelectedItem.ID, skillType, selector.BooleanParameter);
        }

        public async Task AsyncLoadSkill(int id1, CUS_File.SkillType skillType, bool onlyCpk, int replaceItemIndex = -1)
        {
            string message = $"Loading skill \"{Xenoverse2.Instance.GetSkillName(skillType, CUS_File.ConvertToID2(id1, skillType), id1.ToString(), xv2.Language.English)}\"";
            ProgressDialogController progressBarController = await window.ShowProgressAsync("Loading", message, false, DialogSettings.Default);
            progressBarController.SetIndeterminate();

            try
            {
                await Task.Run(async () =>
                {
                    if (GetCmnMove() == null)
                    {
                        await AsyncLoadCmnFiles(progressBarController);
                        progressBarController.SetMessage(message);
                    }

                    Xv2Skill skill = xv2.Instance.GetSkill(skillType, id1, true, onlyCpk);

                    //Add to outliner
                    if (skill != null)
                    {
                        Move move = new Move(skill, skillType);

                        VerifyValues(move.Files);

                        if (move != null)
                        {
                            AddOutlinerItem(new OutlinerItem(move, false, OutlinerItem.OutlinerItemType.Skill, onlyCpk), replaceItemIndex);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Add($"Load Error: {ex.Message}", LogType.Error);
                ExceptionDispatchInfo.Capture(ex).Throw();
                //ExceptionDispatchInfo.Capture(ex.InnerException != null ? ex.InnerException : ex).Throw();
            }
            finally
            {
                await progressBarController.CloseAsync();
            }
        }

        public async void AsyncLoadCharacter()
        {
            var characters = xv2.Instance.GetCharacterList();
            EntitySelector charaSel = new EntitySelector(characters, "Character");
            charaSel.ShowDialog();

            if (charaSel.SelectedItem != null)
            {
                //PartSetSelector partSel = new PartSetSelector(xv2.Instance.GetBcsFile(charaSel.SelectedItem.ID), Application.Current.MainWindow);
                //partSel.ShowDialog();

                //if (partSel.SelectedPartSet != null)
                //    await AsyncLoadCharacter(charaSel.SelectedItem.ID, partSel.SelectedPartSet.ID);

                BCS_File bcsFile = xv2.Instance.GetBcsFile(charaSel.SelectedItem.ID);
                _ = await AsyncLoadCharacter(charaSel.SelectedItem.ID, bcsFile.PartSets.Min(x => x.ID));
            }
        }

        public async Task<Actor> AsyncLoadCharacter(int id, int partSetId, bool readOnly = false, int replaceItemIndex = -1)
        {
            string message = $"Loading character \"{Xenoverse2.Instance.GetCharacterName(id, xv2.Language.English)}\"";
            var progressBarController = await window.ShowProgressAsync("Loading", message, false, DialogSettings.Default);
            progressBarController.SetIndeterminate();

            Actor chara = null;

            try
            {
                if (GetCmnMove() == null)
                {
                    await AsyncLoadCmnFiles(progressBarController);
                    progressBarController.SetMessage(message);
                }

                await Task.Run(() =>
                {
                    Xv2Character xv2Character = xv2.Instance.GetCharacter(id);

                    chara = new Actor(SceneManager.MainGameBase, xv2Character, partSetId);

                    VerifyValues(chara.Moveset.Files);

                    AddOutlinerItem(new OutlinerItem(chara, readOnly, OutlinerItem.OutlinerItemType.Character), replaceItemIndex);
                });
            }
            catch (Exception ex)
            {
                Log.Add($"Load Error: {ex.Message}", LogType.Error);
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
            finally
            {
                await progressBarController.CloseAsync();
            }

            return chara;
        }

        public Actor LoadCharacter(int id, int partSetId, PartSet _partSet = null, bool readOnly = false)
        {
            Xv2Character xv2Character = xv2.Instance.GetCharacter(id);

            Actor chara = new Actor(SceneManager.MainGameBase, xv2Character, _partSet != null ? _partSet.ID : 0);

            VerifyValues(chara.Moveset.Files);

            AddOutlinerItem(new OutlinerItem(chara, readOnly, OutlinerItem.OutlinerItemType.Character));

            return chara;
        }

        public void LoadMoveset()
        {
            var movesets = xv2.Instance.GetCharacterList();
            EntitySelector selector = new EntitySelector(movesets, "Moveset");
            selector.ShowDialog();

            if (selector.SelectedItem != null)
                LoadMoveset(selector.SelectedItem.ID);
        }

        public void LoadMoveset(int id)
        {
            Xv2Character character = xv2.Instance.GetCharacter(id);

            if (character != null)
            {
                Move move = new Move(character);
                VerifyValues(move.Files);

                if (move != null)
                {
                    AddOutlinerItem(new OutlinerItem(move, false, OutlinerItem.OutlinerItemType.Moveset, false));
                }
            }
        }

        private void LoadCmnFiles()
        {
            Move move = new Move();
            move.SetName("CMN");
            move.MoveType = Move.Type.CMN;
            move.Files = new Xv2MoveFiles();

            move.Files.BdmFile = new Xv2File<BDM_File>((BDM_File)file.Instance.GetParsedFileFromGame(xv2.CMN_BDM_PATH), file.Instance.GetAbsolutePath(xv2.CMN_BDM_PATH), false, null, false, xv2.MoveFileTypes.BDM, 0, true, xv2.MoveType.Common);
            move.Files.BsaFile = new Xv2File<BSA_File>((BSA_File)file.Instance.GetParsedFileFromGame(xv2.CMN_BSA_PATH), file.Instance.GetAbsolutePath(xv2.CMN_BSA_PATH), false, null, false, xv2.MoveFileTypes.BSA, 0, true, xv2.MoveType.Common);
            move.Files.ShotBdmFile = new Xv2File<BDM_File>((BDM_File)file.Instance.GetParsedFileFromGame(xv2.CMN_SHOT_BDM_PATH), file.Instance.GetAbsolutePath(xv2.CMN_SHOT_BDM_PATH), false, null, false, xv2.MoveFileTypes.SHOT_BDM, 0, true, xv2.MoveType.Common);
            move.Files.SeAcbFile.Add(new Xv2File<ACB_Wrapper>((ACB_Wrapper)file.Instance.GetParsedFileFromGame(xv2.CMN_SE_ACB_PATH), file.Instance.GetAbsolutePath(xv2.CMN_SE_ACB_PATH), false, null, false, xv2.MoveFileTypes.SE_ACB, 0, true, xv2.MoveType.Common));

            //Load CMN EANs
            move.Files.EanFile.Add(new Xv2File<EAN_File>((EAN_File)file.Instance.GetParsedFileFromGame(xv2.CMN_EAN_PATH), file.Instance.GetAbsolutePath(xv2.CMN_EAN_PATH), false, null, false, xv2.MoveFileTypes.EAN, (int)BAC_Type0.EanTypeEnum.Common, true, xv2.MoveType.Common));
            move.Files.EanFile.Add(new Xv2File<EAN_File>((EAN_File)file.Instance.GetParsedFileFromGame("chara/CMN/MCM.DBA.ean"), file.Instance.GetAbsolutePath("chara/CMN/MCM.DBA.ean"), false, null, false, xv2.MoveFileTypes.EAN, (int)BAC_Type0.EanTypeEnum.MCM_DBA, true, xv2.MoveType.Common));
            move.Files.EanFile.Add(new Xv2File<EAN_File>((EAN_File)file.Instance.GetParsedFileFromGame(xv2.CMN_TAL_EAN), file.Instance.GetAbsolutePath(xv2.CMN_TAL_EAN), false, null, false, xv2.MoveFileTypes.TAL_EAN, (int)BAC_Type0.EanTypeEnum.CommonTail, true, xv2.MoveType.Common));

            //Load CMN CAMs
            move.Files.CamEanFile.Add(new Xv2File<EAN_File>((EAN_File)file.Instance.GetParsedFileFromGame(xv2.CMN_CAM_EAN_PATH), file.Instance.GetAbsolutePath(xv2.CMN_CAM_EAN_PATH), false, null, false, xv2.MoveFileTypes.CAM_EAN, (int)BAC_Type10.EanTypeEnum.Common, true, xv2.MoveType.Common));
            move.Files.CamEanFile.Add(new Xv2File<EAN_File>((EAN_File)file.Instance.GetParsedFileFromGame("chara/CMN/MCM.cam.ean"), file.Instance.GetAbsolutePath("chara/CMN/MCM.cam.ean"), false, null, false, xv2.MoveFileTypes.CAM_EAN, (int)BAC_Type10.EanTypeEnum.MCM, true, xv2.MoveType.Common));

            //Load CMN BACs
            move.Files.BacFiles.Add(new Xv2File<BAC_File>((BAC_File)file.Instance.GetParsedFileFromGame(xv2.CMN_BAC_PATH), file.Instance.GetAbsolutePath(xv2.CMN_BAC_PATH), false, null, false, xv2.MoveFileTypes.BAC, 0, true, xv2.MoveType.Common));
            move.Files.BacFiles.Add(new Xv2File<BAC_File>((BAC_File)file.Instance.GetParsedFileFromGame(xv2.CMN_DBA_BAC_PATH), file.Instance.GetAbsolutePath(xv2.CMN_DBA_BAC_PATH), false, null, false, xv2.MoveFileTypes.BAC, 1, true, xv2.MoveType.Common));
            move.Files.BacFiles.Add(new Xv2File<BAC_File>((BAC_File)file.Instance.GetParsedFileFromGame(xv2.CMN_QEA_BAC_PATH), file.Instance.GetAbsolutePath(xv2.CMN_QEA_BAC_PATH), false, null, false, xv2.MoveFileTypes.BAC, 2, true, xv2.MoveType.Common));
            move.Files.BacFiles.Add(new Xv2File<BAC_File>((BAC_File)file.Instance.GetParsedFileFromGame(xv2.CMN_M_BAC_PATH), file.Instance.GetAbsolutePath(xv2.CMN_M_BAC_PATH), false, null, false, xv2.MoveFileTypes.BAC, 3, true, xv2.MoveType.Common));

            //Load CMN EEPKs
            foreach (var commonEepk in xv2.Instance.ErsFile.GetSubentryList(0))
            {
                if (commonEepk.ID >= 10) break; //Skip all the lobby EEPKs

                string path = $"vfx/{commonEepk.FILE_PATH}";
                move.Files.EepkFiles.Add(new Xv2File<EffectContainerFile>((EffectContainerFile)file.Instance.GetParsedFileFromGame(path), file.Instance.GetAbsolutePath(path), false, null, false, xv2.MoveFileTypes.EEPK, commonEepk.ID, true, xv2.MoveType.Common));
            }

            move.Files.BacFiles[0].File.InitializeIBacTypes();
            move.Files.BacFiles[1].File.InitializeIBacTypes();
            move.Files.BacFiles[2].File.InitializeIBacTypes();
            move.Files.BacFiles[3].File.InitializeIBacTypes();
            move.Files.BsaFile.File.InitializeIBsaTypes();

            VerifyValues(move.Files);

            var existing = OutlinerItems.FirstOrDefault(x => x.Type == OutlinerItem.OutlinerItemType.CMN);

            if (existing != null)
            {
                existing.move = move;
            }
            else
            {
                OutlinerItems.Insert(1, new OutlinerItem(move, true, OutlinerItem.OutlinerItemType.CMN, false));
            }
        }

        private void VerifyValues(Xv2MoveFiles moveFiles)
        {
            //Check flags
            string bacFlags = moveFiles.BacFile.File.ValidateValues();

            if (bacFlags != null)
                Log.Add($"BAC File: Found unknown value in {bacFlags}.", LogType.Warning);

            //Update dictionaries
            BAC.AddMissing(moveFiles.BacFile?.File);
        }


        public async Task AsyncLoadCac(int cacIndex, Xv2CoreLib.SAV.CaC cac)
        {
            var progressBarController = await window.ShowProgressAsync("Loading", $"Loading avatar \"{cac.Name}\"", false, DialogSettings.Default);
            progressBarController.SetIndeterminate();

            try
            {
                await Task.Run(() =>
                {
                    var item = new OutlinerItem(cacIndex, cac);
                    item.CustomAvatar.InitActor();
                    item.CustomAvatar.SetActorAppearence();
                    item.CustomAvatar.SetCustomColors();
                    item.CustomAvatar.SetActorSize();

                    AddOutlinerItem(item);
                });
            }
            catch (Exception ex)
            {
                Log.Add($"Load Error: {ex.Message}", LogType.Error);
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
            finally
            {
                await progressBarController.CloseAsync();
            }
        }
        #endregion

        #region Save
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

        #endregion

        #region GetHelpers
        public Move GetCmnMove()
        {
            var move = OutlinerItems.FirstOrDefault(x => x.Type == OutlinerItem.OutlinerItemType.CMN);
            return move != null ? move.move : null;
        }

        /// <summary>
        /// Get a read-only list of all characters in the current scene.
        /// </summary>
        /// <returns></returns>
        public List<Actor> GetLoadedCharacters()
        {
            List<Actor> chars = new List<Actor>();

            foreach (OutlinerItem item in OutlinerItems.Where(x => x.Type == OutlinerItem.OutlinerItemType.Character || x.Type == OutlinerItem.OutlinerItemType.CaC))
                chars.Add(item.character);

            return chars;
        }

        /// <summary>
        /// Returns the file instance associated with path, if it has been previously loaded.
        /// </summary>
        public object TryGetFileInstance(string path)
        {
            foreach (var outlinerItem in OutlinerItems)
            {
                if (outlinerItem.move != null)
                {
                    var ret = outlinerItem.move.TryGetFileInstance(path);
                    if (ret != null) return ret;
                }
                else if (outlinerItem.character?.Moveset != null)
                {
                    var ret = outlinerItem.character?.Moveset.TryGetFileInstance(path);
                    if (ret != null) return ret;
                }
            }

            return null;
        }

        //File Get
        /// <summary>
        /// Get the Ean File declared by a BAC_Type0 (Animation).
        /// </summary>
        /// <param name="eanType">The ean type.</param>
        /// <param name="move">The skill object. If eanType is for a skill, then the ean from this will be returned.</param>
        /// <param name="character">The chracter object. If eanType is for a character, then the ean from this will be returned. Also used in getting the correct character unique skill EAN file (if any).</param>
        /// <param name="logErrors">Log errors?</param>
        /// <param name="charaUnique">Use character unique EAN if present.</param>
        public EAN_File GetEanFile(BAC_Type0.EanTypeEnum eanType, Move move, Actor character, bool logErrors, bool charaUnique)
        {
            switch (eanType)
            {
                case BAC_Type0.EanTypeEnum.Common:
                case BAC_Type0.EanTypeEnum.MCM_DBA:
                    return Instance.GetCmnMove().Files.EanFile.FirstOrDefault(x => x.Costumes.Contains((int)eanType))?.File;
                case BAC_Type0.EanTypeEnum.Character:
                    if (move == null) return character?.Moveset.Files.GetEanFile(character.ShortName, charaUnique);
                    return (move.MoveType == Move.Type.Moveset) ? move.Files.GetEanFile(character?.ShortName, charaUnique) : character?.Moveset.Files.GetEanFile(character.ShortName, charaUnique);
                case BAC_Type0.EanTypeEnum.Skill:
                    if (move == null)
                    {
                        if (logErrors) Log.Add("Files.GetEanFile: Cannot use skill EAN file from this bac. No attached EAN was found.", LogType.Warning);
                        return null;
                    }
                    if (move.MoveType == Move.Type.Moveset)
                    {
                        if (logErrors) Log.Add("Files.GetEanFile: Cannot use skill EAN file from a moveset.", LogType.Warning);
                        return null;
                    }
                    return move.Files.GetEanFile(character?.ShortName, charaUnique);
                case BAC_Type0.EanTypeEnum.FaceBase:
                    {
                        if (character == null) return null;
                        var faceEan = character.CharacterData.IsCaC ? character.FceEanFile : character.FceEanFile;

                        if (faceEan == null && logErrors)
                        {
                            Log.Add($"Face Ean file was requested for {character.Name}, but none was loaded with the character!", LogType.Warning);
                        }

                        return faceEan;
                    }
                case BAC_Type0.EanTypeEnum.FaceForehead:
                    {
                        if (character == null) return null;
                        var faceEan = character.CharacterData.IsCaC ? character.FceEyeEanFile : character.FceEanFile;

                        if (faceEan == null && logErrors)
                        {
                            Log.Add($"Face Ean file was requested for {character.Name}, but none was loaded with the character!", LogType.Warning);
                        }

                        return faceEan;
                    }
                case BAC_Type0.EanTypeEnum.CommonTail:
                    return Files.Instance.GetCmnMove().Files.GetTailEanFile();
            }

            if (logErrors) Log.Add($"Files.GetEanFile: Could not find the ean file for \"{eanType}\".", LogType.Warning);
            return null;
        }

        /// <summary>
        /// Get the cam.ean file declared by a BAC_Type10 (Camera).
        /// </summary>
        /// <param name="eanType">The ean type.</param>
        /// <param name="move">The skill object. If eanType is for a skill, then the ean from this will be returned.</param>
        /// <param name="character">The chracter object. If eanType is for a character, then the ean from this will be returned. Also used in getting the correct character unique skill EAN file (if any).</param>
        /// <param name="logErrors">Log errors?</param>
        /// <param name="charaUnique">Use character unique EAN if present.</param>
        public EAN_File GetCamEanFile(BAC_Type10.EanTypeEnum eanType, Move move, Actor character, bool logErrors, bool charaUnique)
        {
            //Dont log errors for unknown/unimplemented cam.ean files (too much needless log spam)
            if (eanType != BAC_Type10.EanTypeEnum.Character && eanType != BAC_Type10.EanTypeEnum.Common && eanType != BAC_Type10.EanTypeEnum.Skill)
                logErrors = false;

            switch (eanType)
            {
                case BAC_Type10.EanTypeEnum.Common:
                case BAC_Type10.EanTypeEnum.MCM:
                    return Instance.GetCmnMove().Files.CamEanFile.FirstOrDefault(x => x.Costumes.Contains((int)eanType))?.File;
                case BAC_Type10.EanTypeEnum.Character:
                    if (move == null) return character.Moveset.Files.GetCamEanFile(character.ShortName, charaUnique);
                    return (move.MoveType == Move.Type.Moveset) ? move.Files.GetCamEanFile(character.ShortName, charaUnique) : character.Moveset.Files.GetCamEanFile(character.ShortName, charaUnique);
                case BAC_Type10.EanTypeEnum.Skill:
                    if (move == null)
                    {
                        if (logErrors) Log.Add("Files.GetCamFile: Cannot use skill EAN file from this bac. No attached ean was found.", LogType.Warning);
                        return null;
                    }
                    if (move.MoveType == Move.Type.Moveset)
                    {
                        if (logErrors) Log.Add("Files.GetCamFile: Cannot use skill EAN file from a moveset.", LogType.Warning);
                        return null;
                    }
                    return move.Files.GetCamEanFile(character?.ShortName, charaUnique);
            }

            if (logErrors) Log.Add($"Files.GetCamFile: Could not find the ean file for \"{eanType}\".", LogType.Warning);
            return null;
        }

        public BDM_File GetBdmFile(BAC_Type1.BdmType bdmType, Move move, Actor character, bool logErrors)
        {
            switch (bdmType)
            {
                case BAC_Type1.BdmType.Common:
                    return Instance.GetCmnMove().Files.BdmFile.File;
                case BAC_Type1.BdmType.Character:
                    if (move.MoveType == Move.Type.Skill || move.MoveType == Move.Type.CMN)
                    {
                        if (character == null && logErrors)
                            Log.Add("Files.GetBdmFile: BDM Type is Character but no character was passed in as a parameter, cannot return BDM file.", LogType.Warning);
                        return character?.Moveset.Files.BdmFile.File;
                    }
                    return move?.Files.BdmFile.File;
                case BAC_Type1.BdmType.Skill:
                    if (move.MoveType == Move.Type.Moveset)
                    {
                        if (logErrors) Log.Add("Files.GetBdmFile: Cannot use skill BDM file from a moveset.", LogType.Warning);
                        return null;
                    }
                    return move?.Files.BdmFile.File;
                default:
                    return null;

            }
        }

        public EffectContainerFile GetEepkFile(BAC_Type8.EepkTypeEnum eepkType, ushort skillId, Move move, Actor character, bool logErrors)
        {
            switch (eepkType)
            {
                case BAC_Type8.EepkTypeEnum.AwokenSkill: //AnySkill
                case BAC_Type8.EepkTypeEnum.SuperSkill:
                case BAC_Type8.EepkTypeEnum.UltimateSkill:
                case BAC_Type8.EepkTypeEnum.EvasiveSkill:
                case BAC_Type8.EepkTypeEnum.KiBlastSkill:
                case BAC_Type8.EepkTypeEnum.NEW_AwokenSkill: //The real awoken skill type
                    return (skillId == move.SkillID && move.MoveType == Move.Type.Skill) ? move.Files.EepkFile.File : null;
                case BAC_Type8.EepkTypeEnum.Character:
                    if (move.MoveType == Move.Type.Moveset) return move.Files.EepkFile.File;
                    if (character == null && logErrors)
                        Log.Add("Files.GetEepkFile: EEPK Type is Character but no character was passed in as a parameter, cannot return EEPK file.", LogType.Warning);
                    return character.Moveset.Files.EepkFile.File;
                case BAC_Type8.EepkTypeEnum.Common:
                    Xv2File<EffectContainerFile> eepk = GetCmnMove().Files.EepkFiles.FirstOrDefault(x => x.Costumes.Contains(skillId));
                    return eepk?.File;
            }
            return null;
        }

        public BSA_File GetBsaFile(BAC_Type9.BsaTypeEnum bsaType, ushort skillId, Move move, Actor character, bool logErrors)
        {
            switch (bsaType)
            {
                case BAC_Type9.BsaTypeEnum.Common:
                    return GetCmnMove().Files.BsaFile.File;
                case BAC_Type9.BsaTypeEnum.AwokenSkill:
                case BAC_Type9.BsaTypeEnum.EvasiveSkill:
                case BAC_Type9.BsaTypeEnum.KiBlastSkill:
                case BAC_Type9.BsaTypeEnum.SuperSkill:
                case BAC_Type9.BsaTypeEnum.UltimateSkill:
                    if (move.SkillID == skillId && move.MoveType == Move.Type.Skill) return move.Files.BsaFile.File;
                    return null;
            }
            return null;
        }

        public ACB_Wrapper GetAcbFile(Xv2CoreLib.BAC.AcbType acbType, Move move, Actor character, bool logErrors)
        {
            switch (acbType)
            {
                case Xv2CoreLib.BAC.AcbType.Common_SE:
                    return GetCmnMove().Files.GetSeFile();
                case Xv2CoreLib.BAC.AcbType.Character_SE:
                    return (move.MoveType == Move.Type.Skill || move.MoveType == Move.Type.CMN) ? character?.Moveset.Files.GetSeFile() : move.Files.GetSeFile();
                case Xv2CoreLib.BAC.AcbType.Skill_SE:
                    return move.Files.GetSeFile();
                case Xv2CoreLib.BAC.AcbType.Skill_VOX:
                    if (character == null && logErrors)
                        Log.Add("Files.GetAcbFile: Cannot get Skill_VOX ACB as no character is present!", LogType.Error);

                    return move?.Files.GetVoxFile(character.SkillVoiceAlias != null ? character.SkillVoiceAlias : character.ShortName, 0, true);
                case Xv2CoreLib.BAC.AcbType.Character_VOX:
                    if (character == null && logErrors)
                        Log.Add("Files.GetAcbFile: Cannot get Character_VOX ACB as no character is present!", LogType.Error);
                    return character?.Moveset.Files.GetVoxFile(character.Voice, true);

            }
            return null;
        }
        #endregion


    }
}
