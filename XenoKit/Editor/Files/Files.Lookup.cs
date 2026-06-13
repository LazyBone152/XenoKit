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
        public Move GetCmnMove()
        {
            var move = OutlinerItems.FirstOrDefault(x => x.Type == OutlinerItem.OutlinerItemType.CMN);
            return move != null ? move.move : null;
        }

        public List<Actor> GetLoadedCharacters()
        {
            List<Actor> chars = new List<Actor>();

            foreach (OutlinerItem item in OutlinerItems.Where(x => x.Type == OutlinerItem.OutlinerItemType.Character || x.Type == OutlinerItem.OutlinerItemType.CaC))
                chars.Add(item.character);

            return chars;
        }

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

        public EAN_File GetEanFile(BAC_Type0.EanTypeEnum eanType, Move move, Actor character, bool logErrors, bool charaUnique)
        {
            switch (eanType)
            {
                case BAC_Type0.EanTypeEnum.Common:
                case BAC_Type0.EanTypeEnum.MCM_DBA:
                case BAC_Type0.EanTypeEnum.MCM_TTL:
                case BAC_Type0.EanTypeEnum.MCM_TU6:
                case BAC_Type0.EanTypeEnum.MCM_TU13_5:
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
                case BAC_Type8.EepkTypeEnum.StageBG:
                case BAC_Type8.EepkTypeEnum.Stage:
                    return GetStageEepkFile(eepkType, logErrors);
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

        public EffectContainerFile GetStageEepkFile(BAC_Type8.EepkTypeEnum eepkType, bool logErrors)
        {
            string stageCode = Viewport.Instance?.CurrentStage?.StageDefEntry?.CODE;
            List<string> paths = new List<string>();

            if (!string.IsNullOrWhiteSpace(stageCode))
            {
                if (eepkType == BAC_Type8.EepkTypeEnum.StageBG)
                    paths.Add($"vfx/bg/{stageCode}/BG_{stageCode}.eepk");
                else
                    paths.Add($"vfx/stage/{stageCode}/{stageCode}.eepk");
            }

            if (eepkType == BAC_Type8.EepkTypeEnum.StageBG)
                paths.Add("vfx/bg/LND/BG_LND.eepk");

            foreach (string path in paths)
            {
                EffectContainerFile eepk = GetCachedStageEepk(path, logErrors);
                if (eepk != null) return eepk;
            }

            return null;
        }

        private EffectContainerFile GetCachedStageEepk(string path, bool logErrors)
        {
            if (stageEepkCache.TryGetValue(path, out EffectContainerFile cachedFile))
                return cachedFile;

            try
            {
                EffectContainerFile eepk = file.Instance.GetParsedFileFromGame(path, false, false) as EffectContainerFile;
                if (eepk != null)
                {
                    stageEepkCache[path] = eepk;
                    return eepk;
                }
            }
            catch
            {
            }

            if (logErrors && missingStageEepks.Add(path))
                Log.Add($"Files.GetStageEepkFile: Could not find stage EEPK \"{path}\".", LogType.Warning);

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

    }
}
