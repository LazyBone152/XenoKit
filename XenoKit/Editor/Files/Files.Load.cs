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
                        Log.Add("NSK files can only be loaded in Viewer Mode.", LogType.Info);
                        return;
                    case ".spm":
                        SceneManager.SetDefaultSpm(SPM_File.Load(drop));
                        Log.Add($"Default SPM set to \"{Path.GetFileName(drop)}\"");
                        break;
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
            AddOutlinerItem(new OutlinerItem(filePath));
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
            charaSel.SetBooleanParameter("Only Load From CPK", "Ignore loose files and load directly from CPK.");
            charaSel.ShowDialog();

            if (charaSel.SelectedItem != null)
            {
                //PartSetSelector partSel = new PartSetSelector(xv2.Instance.GetBcsFile(charaSel.SelectedItem.ID), Application.Current.MainWindow);
                //partSel.ShowDialog();

                //if (partSel.SelectedPartSet != null)
                //    await AsyncLoadCharacter(charaSel.SelectedItem.ID, partSel.SelectedPartSet.ID);

                BCS_File bcsFile = xv2.Instance.GetBcsFile(charaSel.SelectedItem.ID, charaSel.BooleanParameter);
                _ = await AsyncLoadCharacter(charaSel.SelectedItem.ID, bcsFile.PartSets.Min(x => x.ID), false, -1, charaSel.BooleanParameter);
            }
        }

        public async Task<Actor> AsyncLoadCharacter(int id, int partSetId, bool readOnly = false, int replaceItemIndex = -1, bool onlyLoadFromCpk = false)
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
                    Xv2Character xv2Character = xv2.Instance.GetCharacter(id, true, onlyLoadFromCpk);

                    chara = new Actor(xv2Character, partSetId);

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

        public Actor LoadCharacter(int id, int partSetId, PartSet _partSet = null, bool readOnly = false, bool onlyLoadFromCpk = false)
        {
            Xv2Character xv2Character = xv2.Instance.GetCharacter(id, true, onlyLoadFromCpk);

            Actor chara = new Actor(xv2Character, _partSet != null ? _partSet.ID : 0);

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

        private async void LoadCmnFiles()
        {
            try
            {
                //Load all basic files immediately, and load the rest in background threads

                List<Task> tasks = new List<Task>();

                Log.Add("CMN load started at " + window.sw.Elapsed, LogType.Debug);
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

                //Load CMN CAMs
                move.Files.CamEanFile.Add(new Xv2File<EAN_File>((EAN_File)file.Instance.GetParsedFileFromGame(xv2.CMN_CAM_EAN_PATH), file.Instance.GetAbsolutePath(xv2.CMN_CAM_EAN_PATH), false, null, false, xv2.MoveFileTypes.CAM_EAN, (int)BAC_Type10.EanTypeEnum.Common, true, xv2.MoveType.Common));

                //Load CMN BACs
                move.Files.BacFiles.Add(new Xv2File<BAC_File>((BAC_File)file.Instance.GetParsedFileFromGame(xv2.CMN_BAC_PATH), file.Instance.GetAbsolutePath(xv2.CMN_BAC_PATH), false, null, false, xv2.MoveFileTypes.BAC, 0, true, xv2.MoveType.Common));
                BAC.AddDefaultMovesetNamesToBac(move.Files.BacFiles[0].File);

                tasks.Add(Task.Run(() =>
                {
                    move.Files.EanFile.Add(new Xv2File<EAN_File>((EAN_File)file.Instance.GetParsedFileFromGame(xv2.CMN_MCM_DBA_EAN_PATH), file.Instance.GetAbsolutePath(xv2.CMN_MCM_DBA_EAN_PATH), false, null, false, xv2.MoveFileTypes.EAN, (int)BAC_Type0.EanTypeEnum.MCM_DBA, true, xv2.MoveType.Common));
                    move.Files.EanFile.Add(new Xv2File<EAN_File>((EAN_File)file.Instance.GetParsedFileFromGame(xv2.CMN_MCM_TTL_EAN_PATH), file.Instance.GetAbsolutePath(xv2.CMN_MCM_TTL_EAN_PATH), false, null, false, xv2.MoveFileTypes.EAN, (int)BAC_Type0.EanTypeEnum.MCM_TTL, true, xv2.MoveType.Common));
                    move.Files.EanFile.Add(new Xv2File<EAN_File>((EAN_File)file.Instance.GetParsedFileFromGame(xv2.CMN_MCM_TU6_EAN_PATH), file.Instance.GetAbsolutePath(xv2.CMN_MCM_TU6_EAN_PATH), false, null, false, xv2.MoveFileTypes.EAN, (int)BAC_Type0.EanTypeEnum.MCM_TU6, true, xv2.MoveType.Common));
                    move.Files.EanFile.Add(new Xv2File<EAN_File>((EAN_File)file.Instance.GetParsedFileFromGame(xv2.CMN_MCM_TU13_5_EAN_PATH), file.Instance.GetAbsolutePath(xv2.CMN_MCM_TU13_5_EAN_PATH), false, null, false, xv2.MoveFileTypes.EAN, (int)BAC_Type0.EanTypeEnum.MCM_TU13_5, true, xv2.MoveType.Common));
                    move.Files.EanFile.Add(new Xv2File<EAN_File>((EAN_File)file.Instance.GetParsedFileFromGame(xv2.CMN_TAL_EAN_PATH), file.Instance.GetAbsolutePath(xv2.CMN_TAL_EAN_PATH), false, null, false, xv2.MoveFileTypes.TAL_EAN, (int)BAC_Type0.EanTypeEnum.CommonTail, true, xv2.MoveType.Common));

                    move.Files.CamEanFile.Add(new Xv2File<EAN_File>((EAN_File)file.Instance.GetParsedFileFromGame(xv2.CMN_MCM_CAM_EAN_PATH), file.Instance.GetAbsolutePath(xv2.CMN_MCM_CAM_EAN_PATH), false, null, false, xv2.MoveFileTypes.CAM_EAN, (int)BAC_Type10.EanTypeEnum.MCM, true, xv2.MoveType.Common));

                    move.Files.BacFiles.Add(new Xv2File<BAC_File>((BAC_File)file.Instance.GetParsedFileFromGame(xv2.CMN_DBA_BAC_PATH), file.Instance.GetAbsolutePath(xv2.CMN_DBA_BAC_PATH), false, null, false, xv2.MoveFileTypes.BAC, 1, true, xv2.MoveType.Common));
                    move.Files.BacFiles.Add(new Xv2File<BAC_File>((BAC_File)file.Instance.GetParsedFileFromGame(xv2.CMN_QEA_BAC_PATH), file.Instance.GetAbsolutePath(xv2.CMN_QEA_BAC_PATH), false, null, false, xv2.MoveFileTypes.BAC, 2, true, xv2.MoveType.Common));
                    move.Files.BacFiles.Add(new Xv2File<BAC_File>((BAC_File)file.Instance.GetParsedFileFromGame(xv2.CMN_M_BAC_PATH), file.Instance.GetAbsolutePath(xv2.CMN_M_BAC_PATH), false, null, false, xv2.MoveFileTypes.BAC, 3, true, xv2.MoveType.Common));

                    move.Files.BacFiles[1].File.InitializeIBacTypes();
                    move.Files.BacFiles[2].File.InitializeIBacTypes();
                    move.Files.BacFiles[3].File.InitializeIBacTypes();
                }));

                //Load CMN EEPKs
                foreach (var commonEepk in xv2.Instance.ErsFile.GetSubentryList(0))
                {
                    if (commonEepk.ID >= 10) break; //Skip all the lobby EEPKs

                    tasks.Add(Task.Run(() =>
                    {
                        string path = $"vfx/{commonEepk.FILE_PATH}";
                        move.Files.EepkFiles.Add(new Xv2File<EffectContainerFile>((EffectContainerFile)file.Instance.GetParsedFileFromGame(path), file.Instance.GetAbsolutePath(path), false, null, false, xv2.MoveFileTypes.EEPK, commonEepk.ID, true, xv2.MoveType.Common));

                    }));

                    //string path = $"vfx/{commonEepk.FILE_PATH}";
                    //move.Files.EepkFiles.Add(new Xv2File<EffectContainerFile>((EffectContainerFile)file.Instance.GetParsedFileFromGame(path), file.Instance.GetAbsolutePath(path), false, null, false, xv2.MoveFileTypes.EEPK, commonEepk.ID, true, xv2.MoveType.Common));
                }

                move.Files.BacFiles[0].File.InitializeIBacTypes();
                move.Files.BsaFile.File.InitializeIBsaTypes();


                VerifyValues(move.Files);

                lock (OutlinerItems)
                {
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

                //Finish up EEPK loading
                Log.Add("CMN load (partial) finished at " + window.sw.Elapsed, LogType.Debug);
                await Task.WhenAll(tasks);
                Log.Add("CMN load (complete) finished at " + window.sw.Elapsed, LogType.Debug);
                move.Files.EepkFiles.Sort((x, y) => x.Costumes[0] - y.Costumes[0]);
            }
            catch (Exception ex)
            {
                Log.Add("Error while loading CMN files: " + ex.Message + "\n\nThese errors are usually caused by a bad mod install. Try removing/renaming the games data folder and trying again.", LogType.Error);
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

        public async void AsyncLoadStage()
        {
            var stages = xv2.Instance.GetStageList();
            EntitySelector stageSel = new EntitySelector(stages, "Stage");
            stageSel.ShowDialog();

            if (stageSel.SelectedItem != null)
            {

                //string code = xv2.Instance.StageDefFile.Stages[stageSel.SelectedItem.ID].CODE;
                //Xv2Stage stage = new Xv2Stage(Game.Instance, code);
                //AddOutlinerItem(new OutlinerItem(stage));
                await AsyncLoadStage(stageSel.SelectedItem.ID);
            }
        }

        public async Task<Xv2Stage> AsyncLoadStage(int stageIdx)
        {
            if (stageIdx >= xv2.Instance.StageDefFile.Stages.Count) return null;

            string code = xv2.Instance.StageDefFile.Stages[stageIdx].CODE;
            string stageName = xv2.Instance.GetStageName(code);

            string message = $"Loading stage \"{stageName}\"";
            var progressBarController = await window.ShowProgressAsync("Loading", message, false, DialogSettings.Default);
            progressBarController.SetIndeterminate();

            Xv2Stage stage = null;

            try
            {
                await Task.Run(() =>
                {
                    stage = new Xv2Stage(code);

                    if(stage.FmpFile != null && stage.SpmFile != null)
                        AddOutlinerItem(new OutlinerItem(stage));
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

            return stage;
        }

        public async Task AsyncLoadCac(int cacIndex, CaC cac)
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

    }
}
