using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
using xv2Utils = Xv2CoreLib.Utils;
using XenoKit.Engine;
using GalaSoft.MvvmLight.CommandWpf;
using System.Runtime.ExceptionServices;
using Xv2CoreLib.Resource.App;
using Application = System.Windows.Application;
using Xv2CoreLib.Resource;

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
                SelectedMoveChanged.Invoke(null, null);
            }
        }
        public Move SelectedMove
        {
            get
            {
                return SelectedItem?.GetMove();
            }
        }

        public static event EventHandler SelectedMoveChanged;
        
        private Files()
        {

        }

        /// <summary>
        /// Load game CPKs and name list files. A progress bar will appear on window as they load.
        /// </summary>
        public async void Initialize(MetroWindow window)
        {
            this.window = window;

            var controller = await window.ShowProgressAsync("Initializing", "Reading game files...", false, DialogSettings.Default);
            controller.SetIndeterminate();

            try
            {
                await Task.Run(() =>
                {
                    xv2.GameDir = SettingsManager.settings.GameDirectory;
                    xv2.Instance.loadCharacters = true;
                    xv2.Instance.loadSkills = true;
                    xv2.Instance.loadCmn = true;
                    xv2.Instance.Init();
                });


                if (GetCmnMove() == null)
                {
                    controller.SetMessage("Loading common files...");
                    await Task.Run(LoadCmnFiles);
                }
            }
            catch (Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
            finally
            {
                await controller.CloseAsync();
            }
        }

        #region RightClickMenuCommands
        public RelayCommand SaveSelectedItemCommand => new RelayCommand(SaveSelectedItem, CanSave);
        private void SaveSelectedItem()
        {
            SaveItem(_selectedItem);
        }

        private bool CanSave()
        {
            return _selectedItem?.ReadOnly == false;
        }
        #endregion

        #region Load
        public void LoadSkill(CUS_File.SkillType skillType)
        {
            var skills = xv2.Instance.GetSkillList(skillType);
            EntitySelector selector = new EntitySelector(skills, skillType.ToString(), Application.Current.MainWindow);
            selector.ShowDialog();

            if (selector.SelectedItem != null)
                LoadSkill(selector.SelectedItem.ID, skillType);
        }

        public void LoadSkill(int id1, CUS_File.SkillType skillType)
        {
            Xv2Skill skill = xv2.Instance.GetSkill(skillType, id1);

            //Hotswap files if needed:
            //This ensures only a single instance of any file is loaded at any given time, for when skills borrow files from other skills.
            HotswapBorrowedFiles(skill.Files.BdmFile);
            HotswapBorrowedFiles(skill.Files.ShotBdmFile);
            HotswapBorrowedFiles(skill.Files.EepkFile);
            HotswapBorrowedFiles(skill.Files.BsaFile);

            foreach (var file in skill.Files.SeAcbFile)
                HotswapBorrowedFiles(file);

            foreach (var file in skill.Files.VoxAcbFile)
                HotswapBorrowedFiles(file);

            foreach (var file in skill.Files.EanFile)
                HotswapBorrowedFiles(file);

            foreach (var file in skill.Files.CamEanFile)
                HotswapBorrowedFiles(file);

            //Add to outliner
            if (skill != null)
            {
                Move move = new Move(skill, skillType);

                VerifyValues(move.Files);

                if (move != null)
                {
                    OutlinerItems.Add(new OutlinerItem(move, false, OutlinerItem.OutlinerItemType.Skill));
                }
            }
        }

        public void LoadCharacter()
        {
            var characters = xv2.Instance.GetCharacterList();
            EntitySelector charaSel = new EntitySelector(characters, "Character", Application.Current.MainWindow);
            charaSel.ShowDialog();

            if (charaSel.SelectedItem != null)
            {
                var partSets = xv2.Instance.GetPartSetList(charaSel.SelectedItem.ID);
                PartSetSelector partSel = new PartSetSelector(xv2.Instance.GetBcsFile(charaSel.SelectedItem.ID), Application.Current.MainWindow);
                partSel.ShowDialog();

                if (partSel.SelectedPartSet != null)
                    LoadCharacter(charaSel.SelectedItem.ID, -1, partSel.SelectedPartSet);
            }
        }

        public Character LoadCharacter(int id, int partSetId, PartSet _partSet = null, bool readOnly = false)
        {
            Xv2Character xv2Character = xv2.Instance.GetCharacter(id);
            
            string eskPath = xv2Utils.ResolveRelativePath(string.Format("chara/{0}/{1}_000.esk", xv2Character.CmsEntry.ShortName, xv2Character.CmsEntry.BcsPath));
            CharacterSkeleton skeleton = new CharacterSkeleton((ESK_File)xv2.Instance.GetParsedFileFromGame(eskPath));
            var chara = new Character(skeleton, SceneManager.GraphicsDeviceRef, new Vector3(), xv2Character.Name[0]);
            chara.ShortName = xv2Character.CmsEntry?.ShortName;

            //Load PartSet files
            EmdFileInfo fileInfo = new EmdFileInfo();

            var partSet = (_partSet != null) ? _partSet : xv2Character.BcsFile.File.PartSets.FirstOrDefault(x => x.ID == partSetId);
            GetBcsPartFiles(fileInfo, partSet.FaceBase, PartType.FaceBase);
            GetBcsPartFiles(fileInfo, partSet.FaceEye, PartType.FaceEye);
            GetBcsPartFiles(fileInfo, partSet.FaceEar, PartType.FaceEar);
            GetBcsPartFiles(fileInfo, partSet.FaceForehead, PartType.FaceForehead);
            GetBcsPartFiles(fileInfo, partSet.FaceNose, PartType.FaceNose);
            GetBcsPartFiles(fileInfo, partSet.Bust, PartType.Bust);
            GetBcsPartFiles(fileInfo, partSet.Boots, PartType.Boots);
            GetBcsPartFiles(fileInfo, partSet.Pants, PartType.Pants);
            GetBcsPartFiles(fileInfo, partSet.Rist, PartType.Rist);
            GetBcsPartFiles(fileInfo, partSet.Hair, PartType.Hair);

            //Load models
            foreach (var file in fileInfo.EmdPaths)
            {
                chara.AddModel(xv2.Instance.GetBytesFromGame(file), Path.GetFileNameWithoutExtension(file));
            }

            //Load physics objects
            if(fileInfo.PhysicsEmdPaths.Count != fileInfo.PhysicsBoneNames.Count)
            {
                Log.Add("Files.LoadCharacter: physics object path and bones out of sync. Abort load.", LogType.Error);
                return null;
            }

            for (int i = 0; i < fileInfo.PhysicsEmdPaths.Count; i++)
            {
                chara.AddPhysicsModel(fileInfo.PhysicsBoneNames[i], xv2.Instance.GetBytesFromGame(fileInfo.PhysicsEmdPaths[i]));
            }

            chara.Moveset = new Move(xv2Character);
            chara.Type = (short)((id >= 100 && id < 109) ? 1 : 2);
            chara.characterData = xv2Character;

            VerifyValues(chara.Moveset.Files);

            OutlinerItems.Add(new OutlinerItem(chara, readOnly, OutlinerItem.OutlinerItemType.Character));

            return chara;
        }

        public void LoadMoveset()
        {
            var movesets = xv2.Instance.GetCharacterList();
            EntitySelector selector = new EntitySelector(movesets, "Moveset", Application.Current.MainWindow);
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
                    OutlinerItems.Add(new OutlinerItem(move, false, OutlinerItem.OutlinerItemType.Moveset));
                }
            }
        }

        private void HotswapBorrowedFiles<T>(Xv2File<T> xv2File) where T : class
        {
            
            if (xv2File.Borrowed)
            {
                object instance = TryGetFileInstance(xv2File.Path);
                if (instance != null)
                {
                    if(instance.GetType() == typeof(T))
                        xv2File.File = (T)instance;
                }
            }
        }

        private void LoadCmnFiles()
        {
            Move move = new Move();
            move.SetName("CMN");
            move.MoveType = Move.Type.CMN;
            move.Files = new Xv2MoveFiles();

            move.Files.BacFile = new Xv2File<BAC_File>((BAC_File)xv2.Instance.GetParsedFileFromGame(xv2.CMN_BAC_PATH), xv2.Instance.GetAbsolutePath(xv2.CMN_BAC_PATH), false, null, false, xv2.MoveFileTypes.BAC, 0, true, xv2.MoveType.Common);
            move.Files.BdmFile = new Xv2File<BDM_File>((BDM_File)xv2.Instance.GetParsedFileFromGame(xv2.CMN_BDM_PATH), xv2.Instance.GetAbsolutePath(xv2.CMN_BDM_PATH), false, null, false, xv2.MoveFileTypes.BDM, 0, true, xv2.MoveType.Common);
            move.Files.BsaFile = new Xv2File<BSA_File>((BSA_File)xv2.Instance.GetParsedFileFromGame(xv2.CMN_BSA_PATH), xv2.Instance.GetAbsolutePath(xv2.CMN_BSA_PATH), false, null, false, xv2.MoveFileTypes.BSA, 0, true, xv2.MoveType.Common);
            move.Files.ShotBdmFile = new Xv2File<BDM_File>((BDM_File)xv2.Instance.GetParsedFileFromGame(xv2.CMN_SHOT_BDM_PATH), xv2.Instance.GetAbsolutePath(xv2.CMN_SHOT_BDM_PATH), false, null, false, xv2.MoveFileTypes.SHOT_BDM, 0, true, xv2.MoveType.Common);
            move.Files.EanFile.Add(new Xv2File<EAN_File>((EAN_File)xv2.Instance.GetParsedFileFromGame(xv2.CMN_EAN_PATH), xv2.Instance.GetAbsolutePath(xv2.CMN_EAN_PATH), false, null, false, xv2.MoveFileTypes.EAN, 0, true, xv2.MoveType.Common));
            move.Files.CamEanFile.Add(new Xv2File<EAN_File>((EAN_File)xv2.Instance.GetParsedFileFromGame(xv2.CMN_CAM_EAN_PATH), xv2.Instance.GetAbsolutePath(xv2.CMN_CAM_EAN_PATH), false, null, false, xv2.MoveFileTypes.CAM_EAN, 0, true, xv2.MoveType.Common));
            move.Files.EepkFile = new Xv2File<EffectContainerFile>((EffectContainerFile)xv2.Instance.GetParsedFileFromGame(xv2.CMN_EEPK_PATH), xv2.Instance.GetAbsolutePath(xv2.CMN_EEPK_PATH), false, null, false, xv2.MoveFileTypes.EEPK, 0, true, xv2.MoveType.Common);
            move.Files.SeAcbFile.Add(new Xv2File<ACB_Wrapper>((ACB_Wrapper)xv2.Instance.GetParsedFileFromGame(xv2.CMN_SE_ACB_PATH), xv2.Instance.GetAbsolutePath(xv2.CMN_SE_ACB_PATH), false, null, false, xv2.MoveFileTypes.SE_ACB, 0, true, xv2.MoveType.Common));
            move.Files.EanFile.Add(new Xv2File<EAN_File>((EAN_File)xv2.Instance.GetParsedFileFromGame(xv2.CMN_TAL_EAN), xv2.Instance.GetAbsolutePath(xv2.CMN_TAL_EAN), false, null, false, xv2.MoveFileTypes.TAL_EAN, 0, true, xv2.MoveType.Common));

            move.Files.BacFile.File.InitializeIBacTypes();
            move.Files.BsaFile.File.InitializeIBsaTypes();

            VerifyValues(move.Files);

            var existing = OutlinerItems.FirstOrDefault(x => x.Type == OutlinerItem.OutlinerItemType.CMN);

            if (existing != null)
            {
                existing.move = move;
            }
            else
            {
                OutlinerItems.Add(new OutlinerItem(move, true, OutlinerItem.OutlinerItemType.CMN));
            }
        }

        private void VerifyValues(Xv2MoveFiles moveFiles)
        {
            //Check flags
            string bacFlags = moveFiles.BacFile.File.ValidateValues();

            if (bacFlags != null)
                Log.Add($"BAC File: Found unknown value in {bacFlags}.", LogType.Warning);

            //Update dictionaries
            ValuesDictionary.BAC.AddMissing(moveFiles.BacFile?.File);
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

            //Validation
            if (!item.SaveValidate(window))
            {
                Log.Add($"{item.DisplayName} ({item.Type}) save failed due to validation errors", LogType.Error);
                return;
            }


            var progressBarController = await window.ShowProgressAsync("Saving", "Save in progress...", false, DialogSettings.Default);
            progressBarController.SetIndeterminate();

            try
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
                    }
                });

            }
            catch (Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
            finally
            {
                if(log)
                    Log.Add($"{item.DisplayName} ({item.Type}) saved!", LogType.Info);

                await progressBarController.CloseAsync();
            }

        }

        public void SaveAll()
        {
            int count = 0;

            foreach(var file in OutlinerItems)
            {
                if(!file.ReadOnly)
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
            cmn.Files.BacFile?.File?.SaveIBacTypes();
            cmn.Files.BsaFile?.File?.SaveIBsaTypes();

            cmn.ConvertToXv2Skill().SaveMoveFiles();
        }

        private void SaveSkill(OutlinerItem item)
        {
            //Convert IBacTypes/IBsaTypes back into individual lists
            item.GetMove().Files.BacFile?.File?.SaveIBacTypes();
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

            if (item.IsManualLoaded || item.character.characterData.IsCaC)
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
        
        #endregion

        #region GetHelpers
        public Move GetCmnMove()
        {
            var move = OutlinerItems.FirstOrDefault(x => x.Type == OutlinerItem.OutlinerItemType.CMN);
            return move != null ? move.move : null;
        }

        private void GetBcsPartFiles(EmdFileInfo fileInfo, Part part, PartType partType)
        {
            if (part != null)
            {
                string path = part.GetModelPath(partType);

                if (path != null)
                    fileInfo.EmdPaths.Add(part.GetModelPath(partType));

                if (part.Physics_Objects != null)
                {
                    foreach (var physicObject in part.Physics_Objects)
                    {
                        fileInfo.PhysicsEmdPaths.Add(physicObject.GetModelPath(partType));
                        fileInfo.PhysicsBoneNames.Add(physicObject.BoneToAttach);
                    }
                }
            }
        }

        /// <summary>
        /// Get a read-only list of all characters in the current scene.
        /// </summary>
        /// <returns></returns>
        public List<Character> GetLoadedCharacters()
        {
            List<Character> chars = new List<Character>();
            foreach (var item in OutlinerItems.Where(x => x.Type == OutlinerItem.OutlinerItemType.Character))
                chars.Add(item.character);
            return chars;
        }
        
        /// <summary>
        /// Returns the file instance associated with path, if it has been previously loaded.
        /// </summary>
        public object TryGetFileInstance(string path)
        {
            foreach(var outlinerItem in OutlinerItems)
            {
                if(outlinerItem.move != null)
                {
                    var ret = outlinerItem.move.TryGetFileInstance(path);
                    if (ret != null) return ret;
                } 
                else if(outlinerItem.character?.Moveset != null)
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
        public EAN_File GetEanFile(BAC_Type0.EanType eanType, Move move, Character character, bool logErrors, bool charaUnique)
        {
            switch (eanType)
            {
                case BAC_Type0.EanType.Common:
                    return Instance.GetCmnMove().Files.GetEanFile(null, true);
                case BAC_Type0.EanType.Character:
                    if (move == null) return character?.Moveset.Files.GetEanFile(character.ShortName, charaUnique);
                    return (move.MoveType == Move.Type.Moveset) ? move.Files.GetEanFile(character.ShortName, charaUnique) : character?.Moveset.Files.GetEanFile(character.ShortName, charaUnique);
                case BAC_Type0.EanType.Skill:
                    if (move == null)
                    {
                        if(logErrors) Log.Add("Files.GetEanFile: Cannot use skill EAN file from this bac. No attached ean was found.", LogType.Warning);
                        return null;
                    }
                    if (move.MoveType == Move.Type.Moveset)
                    {
                        if (logErrors) Log.Add("Files.GetEanFile: Cannot use skill EAN file from a moveset.", LogType.Warning);
                        return null;
                    }
                    return move.Files.GetEanFile(character?.ShortName, charaUnique);
                case BAC_Type0.EanType.FaceA:
                case BAC_Type0.EanType.FaceB:
                    var faceEan = character.Moveset.Files.GetFaceEanFile();

                    if (faceEan == null && logErrors)
                    {
                        Log.Add($"Face Ean file was requested for {character.Name}, but none was loaded with the character!", LogType.Warning);
                    }

                    return faceEan;
                case BAC_Type0.EanType.CommonTail:
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
        public EAN_File GetCamEanFile(BAC_Type10.EanType eanType, Move move, Character character, bool logErrors, bool charaUnique)
        {
            switch (eanType)
            {
                case BAC_Type10.EanType.Common:
                    return Instance.GetCmnMove().Files.GetCamEanFile(null, charaUnique);
                case BAC_Type10.EanType.Character:
                    if (move == null) return character.Moveset.Files.GetCamEanFile(character.ShortName, charaUnique);
                    return (move.MoveType == Move.Type.Moveset) ? move.Files.GetCamEanFile(character.ShortName, charaUnique) : character.Moveset.Files.GetCamEanFile(character.ShortName, charaUnique);
                case BAC_Type10.EanType.Skill:
                    if (move == null)
                    {
                        if(logErrors) Log.Add("Files.GetCamFile: Cannot use skill EAN file from this bac. No attached ean was found.", LogType.Warning);
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

        public BDM_File GetBdmFile(BAC_Type1.BdmType bdmType, Move move, Character character, bool logErrors)
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
        
        public EffectContainerFile GetEepkFile(BAC_Type8.EepkTypeEnum eepkType, ushort skillId, Move move, Character character, bool logErrors)
        {
            //Currently only returns Skills and Moveset EEPKs
            if (move.MoveType == Move.Type.CMN) return null;

            switch (eepkType)
            {
                case BAC_Type8.EepkTypeEnum.SuperSkill:
                case BAC_Type8.EepkTypeEnum.UltimateSkill:
                case BAC_Type8.EepkTypeEnum.EvasiveSkill:
                case BAC_Type8.EepkTypeEnum.KiBlastSkill:
                case BAC_Type8.EepkTypeEnum.AwokenSkill:
                    //Dont care about checking the type against skill type on the Move, as theses IDs are a bit weird in the game and dont always match up (such as KMH being declared as 3, which is for Awoken skills)
                    return (skillId == move.SkillID && move.MoveType == Move.Type.Skill) ? move.Files.EepkFile.File : null;
                case BAC_Type8.EepkTypeEnum.Character:
                    if (move.MoveType == Move.Type.Moveset) return move.Files.EepkFile.File;
                    if (character == null && logErrors)
                        Log.Add("Files.GetEepkFile: EEPK Type is Character but no character was passed in as a parameter, cannot return EEPK file.", LogType.Warning);
                    return character.Moveset.Files.EepkFile.File;
            }
            return null;
        }

        public BSA_File GetBsaFile(BAC_Type9.BsaTypeEnum bsaType, ushort skillId, Move move, Character character, bool logErrors)
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
        
        public ACB_Wrapper GetAcbFile(Xv2CoreLib.BAC.AcbType acbType, Move move, Character character, bool logErrors)
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
                    return move?.Files.GetVoxFile(character.ShortName, 0, true);
                case Xv2CoreLib.BAC.AcbType.Character_VOX:
                    if (character == null && logErrors)
                        Log.Add("Files.GetAcbFile: Cannot get Character_VOX ACB as no character is present!", LogType.Error);
                    return character?.Moveset.Files.GetVoxFile(0, true);

            }
            return null;
        }
        #endregion
    
    
    }

    public class OutlinerItem : INotifyPropertyChanged
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
        public enum OutlinerItemType
        {
            Character,
            Skill,
            Moveset,
            CMN
        }

        public OutlinerItemType Type { get; set; }
        public bool ReadOnly { get; set; }
        public bool IsManualLoaded { get; set; } = false;

        public Move move { get; set; }
        public Character character { get; set; }

        public string DisplayName
        {
            get
            {
                if (Type == OutlinerItemType.CMN) return "Common";
                return (Type == OutlinerItemType.Character) ? character.Name : move.Name;
            }
        }
        public string DisplayType
        {
            get
            {
                return Type.ToString().ToUpper();
            }
        }


        //Editor Helpers
        /// <summary>
        /// Can files (vox, ean, cam) be added in the editor?
        /// </summary>
        public bool CanAddFiles { get { return (IsManualLoaded || GetMove().MoveType == Move.Type.CMN || GetMove().MoveType == Move.Type.Moveset) ? false : true; } }
        public bool CanUseSystemTab { get { return (GetMove().MoveType == Move.Type.Skill && !IsManualLoaded); } }

        #region WpfVisibility
        public Visibility IsSkill { get { return (Type == OutlinerItemType.Skill) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility IsMoveset { get { return (Type == OutlinerItemType.Moveset) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility IsCharacter { get { return (Type == OutlinerItemType.Character) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility IsCommon { get { return (Type == OutlinerItemType.CMN) ? Visibility.Visible : Visibility.Collapsed; } }

        public Visibility AnimationVisibility { get { return (GetMove().Files.EanFile?.Count > 0) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility StateVisibility { get { return (GetMove().Files.BcmFile != null) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility ActionVisibility { get { return (GetMove().Files.BacFile != null) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility EffectVisibility { get { return (GetMove().Files.EepkFile != null) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility AudioVisibility { get { return (GetMove().Files.SeAcbFile != null || GetMove().Files.VoxAcbFile?.Count > 0) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility HitboxVisibility { get { return (GetMove().Files.BdmFile != null || GetMove().Files.ShotBdmFile != null) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility ProjectileVisibility { get { return (GetMove().Files.BsaFile != null) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility CameraVisibility { get { return (GetMove().Files.CamEanFile != null) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility SystemVisibility { get { return (Type == OutlinerItemType.Skill) ? Visibility.Visible : Visibility.Collapsed; } }
        #endregion

        #region SelectedItems
        private Xv2File<EAN_File> _selectedEanFile = null;
        private Xv2File<EAN_File> _selectedCamFile = null;
        private Xv2File<ACB_Wrapper> _selectedSeAcbFile = null;
        private Xv2File<ACB_Wrapper> _selectedVoxAcbFile = null;
        private EAN_Animation _selectedAnimation = null;
        private EAN_Animation _selectedCamera = null;

        public Xv2File<EAN_File> SelectedEanFile
        {
            get { return _selectedEanFile; }
            set
            {
                if(_selectedEanFile != value)
                {
                    _selectedEanFile = value;
                    NotifyPropertyChanged(nameof(SelectedEanFile));
                }
            }
        }
        public Xv2File<EAN_File> SelectedCamFile
        {
            get { return _selectedCamFile; }
            set
            {
                if (_selectedCamFile != value)
                {
                    _selectedCamFile = value;
                    NotifyPropertyChanged(nameof(SelectedCamFile));
                }
            }
        }
        public Xv2File<ACB_Wrapper> SelectedSeAcbFile
        {
            get { return _selectedSeAcbFile; }
            set
            {
                if (_selectedSeAcbFile != value)
                {
                    _selectedSeAcbFile = value;
                    NotifyPropertyChanged(nameof(SelectedSeAcbFile));
                }
            }
        }
        public Xv2File<ACB_Wrapper> SelectedVoxAcbFile
        {
            get { return _selectedVoxAcbFile; }
            set
            {
                if (_selectedVoxAcbFile != value)
                {
                    _selectedVoxAcbFile = value;
                    NotifyPropertyChanged(nameof(SelectedVoxAcbFile));
                }
            }
        }
        public EAN_Animation SelectedAnimation
        {
            get { return _selectedAnimation; }
            set
            {
                if (_selectedAnimation != value)
                {
                    _selectedAnimation = value;
                    NotifyPropertyChanged(nameof(SelectedAnimation));
                }
            }
        }
        public EAN_Animation SelectedCamera
        {
            get { return _selectedCamera; }
            set
            {
                if (_selectedCamera != value)
                {
                    _selectedCamera = value;
                    NotifyPropertyChanged(nameof(SelectedCamera));
                }
            }
        }
        
        #endregion

        public OutlinerItem(Move move, bool readOnly, OutlinerItemType type)
        {
            this.move = move;
            ReadOnly = readOnly;
            Type = type;
            SetSelectedItems();
        }

        public OutlinerItem(Character chara, bool readOnly, OutlinerItemType type)
        {
            this.character = chara;
            ReadOnly = readOnly;
            Type = type;
            SetSelectedItems();
        }

        /// <summary>
        /// Fix references after pasting from clipboard.
        /// </summary>
        public void FixReferences()
        {
            if(move != null)
            {
                if (move.Files.SeAcbFile != null)
                {
                    foreach (var file in move.Files.SeAcbFile)
                    {
                        file.File.AcbFile.SetCommandTableVersion();
                        file.File = new ACB_Wrapper(file.File.AcbFile);
                    }
                }

                if (move.Files.VoxAcbFile != null)
                {
                    foreach(var file in move.Files.VoxAcbFile)
                    {
                        file.File.AcbFile.SetCommandTableVersion();
                        file.File = new ACB_Wrapper(file.File.AcbFile);
                    }
                }
            }
        }
    
        public Move GetMove()
        {
            return Type == OutlinerItemType.Character ? character.Moveset : move;
        }

        public bool SaveValidate(MetroWindow window)
        {
            if (!GetMove().SaveValidate(window)) return false;

            return true;
        }
    
        /// <summary>
        /// Call after loading - will set default selected items.
        /// </summary>
        private void SetSelectedItems()
        {
            SelectedEanFile = (GetMove().Files.EanFile.Count > 0) ? GetMove().Files.EanFile[0] : null;
            SelectedCamFile = (GetMove().Files.CamEanFile.Count > 0) ? GetMove().Files.CamEanFile[0] : null;
            SelectedSeAcbFile = (GetMove().Files.SeAcbFile.Count > 0) ? GetMove().Files.SeAcbFile[0] : null;
            SelectedVoxAcbFile = (GetMove().Files.VoxAcbFile.Count > 0) ? GetMove().Files.VoxAcbFile[0] : null;
        }
    
        public xv2.MoveType GetMoveType()
        {
            switch (Type)
            {
                case OutlinerItemType.Character:
                case OutlinerItemType.Moveset:
                    return xv2.MoveType.Character;
                case OutlinerItemType.Skill:
                    return xv2.MoveType.Character;
                case OutlinerItemType.CMN:
                    return xv2.MoveType.Common;
                default:
                    return 0;
            }
        }
    }

    public class EmdFileInfo
    {
        public List<string> EmdPaths = new List<string>();
        public List<string> PhysicsEmdPaths = new List<string>();
        public List<string> PhysicsBoneNames = new List<string>();
    }
}
