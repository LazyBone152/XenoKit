using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BCM;
using Xv2CoreLib.BSA;
using Xv2CoreLib.BDM;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EAN;
using Xv2CoreLib.Resource;
using System.IO;
using Xv2CoreLib.CUS;
using static Xv2CoreLib.CUS.CUS_File;
using System.ComponentModel;
using static Xv2CoreLib.BAC.BAC_Type9;
using Xv2CoreLib;
using System.Collections.ObjectModel;
using Xv2CoreLib.PUP;
using Xv2CoreLib.IDB;
using Xv2CoreLib.ACB;
using xv2 = Xv2CoreLib.Xenoverse2;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Xv2CoreLib.CMS;

namespace XenoKit.Editor 
{
    [Serializable]
    public class Move : INotifyPropertyChanged
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

        public enum Type
        {
            Skill,
            Moveset,
            CMN
        }

        //List of all common moveset animations, minus the chara code. These animations wont be copied over by default.
        public static readonly string[] StandardAnimations = new string[]
        {
            "BAS_STAND",
            "BAS_STAND_S",
            "BAS_WALK",
            "BAS_RUN",
            "BAS_JUMP",
            "BAS_JUMP_FRONT_00",
            "BAS_JUMP_FRONT_01",
            "BAS_JUMP_FRONT_02",
            "BAS_JUMP_BACK_00",
            "BAS_JUMP_BACK_01",
            "BAS_JUMP_BACK_02",
            "BAS_BUKUU_UP",
            "BAS_BUKUU_DOWN",
            "BAS_BUKUU_F_S",
            "BAS_BUKUU_B_S",
            "BAS_BUKUU_L_S",
            "BAS_BUKUU_R_S",
            "BAS_BOOST_F_00",
            "BAS_BOOST_F_01",
            "BAS_BOOST_F_02",
            "BAS_STUN_00",
            "BAS_STUN_00_S",
            "BAS_STUN_01",
            "BAS_STUN_01_S",
            "BAS_STUN_02",
            "BAS_STUN_02_S",
            "BAS_STEP_F_S",
            "BAS_STEP_B_S",
            "BAS_STEP_L_S",
            "BAS_STEP_R_S",
            "BAS_STAND_00_S",
            "BAS_STAND_01_S"
        };

        //Metadata
        public Type MoveType = Type.Skill;
        public SkillType SkillType = SkillType.Super;
        public int SkillID = 47818;

        //Skill Data
        public string Name { get { return Names[(int)xv2.Instance.PreferedLanguage]; } }
        public string[] Names { get; set; } = new string[(int)xv2.Language.NumLanguages];
        public string[] Descs { get; set; } = new string[(int)xv2.Language.NumLanguages];
        public ObservableCollection<string[]> AwokenStageNames = new ObservableCollection<string[]>();
        public Xv2MoveFiles Files { get; set;}
        
        public ObservableCollection<PUP_Entry> PupEntries { get; set; } //Optional
        public Skill CusEntry { get; set; } //Required
        public IDB_Entry IdbEntry { get; set; } //Optional

        //Moveset Data (used for saving back to game - nothing else)
        public CMS_Entry CmsEntry { get; set; }


        //Misc
        /// <summary>
        /// The numeric skill type used in various files (bac, bsa, bdm...)
        /// </summary>
        public int NumericSkillType
        {
            get
            {
                if (MoveType == Type.Moveset) return 2;

                switch (SkillType)
                {
                    case SkillType.Awoken:
                        return 3;
                    case SkillType.Super:
                        return 5;
                    case SkillType.Ultimate:
                        return 6;
                    case SkillType.Evasive:
                        return 7;
                    case SkillType.Blast:
                        return 9;
                    default:
                        return 0;
                }
            }
        }
        

        #region Constructors
        public Move() { }

        public Move(Xv2Skill skill, SkillType skillType)
        {
            MoveType = Type.Skill;
            SkillType = skillType;
            Files = skill.Files;
            PupEntries = skill.PupEntries;
            IdbEntry = skill.IdbEntry;
            CusEntry = skill.CusEntry;
            Names = skill.Name;
            Descs = skill.Description;
            AwokenStageNames = skill.BtlHud;
            SkillID = (skill.CusEntry != null) ? skill.CusEntry.ID2 : 47818;

            Files.BacFile.File.InitializeIBacTypes();
            Files.BsaFile.File.InitializeIBsaTypes();

            if (PupEntries == null) PupEntries = new ObservableCollection<PUP_Entry>();
        }

        public Move(Xv2Character character)
        {
            MoveType = Type.Moveset;
            Files = character.MovesetFiles;
            Names = character.Name;
            CmsEntry = character.CmsEntry;

            Files.BacFile.File.InitializeIBacTypes();
        }

        #endregion


        #region Validation
        /// <summary>
        /// Checks if the current Move instance has any outstanding issues that prevent it from being saved safely, and displays an appropriate error message if so.
        /// </summary>
        /// <returns>A bool indicating if the validation passed.</returns>
        public bool SaveValidate(MetroWindow window)
        {
            if(CusEntry != null)
            {
                if (PupEntries.Count > 0 && PupEntries?.Count != CusEntry.NumTransformations)
                {
                    window.ShowMessageAsync($"Invalid PUP Entry Count", $"Pup Entry count must match CUS > NumTransformations or be 0.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                    return false;
                }

                if(CusEntry.ID2 > 5000)
                {
                    window.ShowMessageAsync($"Invalid ID", $"Skill ID must be between 0 and 5000 (currently is: {CusEntry.ID2}).", MessageDialogStyle.Affirmative, DialogSettings.Default);
                    return false;
                }
            }

            //Check for duplicate Arg0 (Skill only)
            if(MoveType == Type.Skill)
            {
                if (!Validate_DuplicateArg0(window, Files.VoxAcbFile, "Chara Code", "VOX ACB")) return false;
                if (!Validate_DuplicateArg0(window, Files.EanFile, "Chara Code", "EAN")) return false;
                if (!Validate_DuplicateArg0(window, Files.CamEanFile, "Chara Code", "CAM EAN")) return false;
            }

            //Check for duplicate entries
            if (!Validate_DuplicateEntries(window, Files.BacFile?.File?.BacEntries, "BAC")) return false;
            if (!Validate_DuplicateEntries(window, Files.BsaFile?.File?.BSA_Entries, "BSA")) return false;
            if (!Validate_DuplicateEntries(window, Files.BdmFile?.File?.BDM_Entries, "BDM")) return false;
            if (!Validate_DuplicateEntries(window, Files.ShotBdmFile?.File?.BDM_Entries, "SHOT.BDM")) return false;
            if (!Validate_DuplicateEntries(window, Files.EepkFile?.File?.Effects, "Effect")) return false;

            foreach (var ean in Files.EanFile)
            {
                if (!Validate_DuplicateEntries(window, ean.File?.Animations, $"Animation ({ean.CharaCode})")) return false;
            }

            foreach (var ean in Files.CamEanFile)
            {
                if (!Validate_DuplicateEntries(window, ean.File?.Animations, $"Camera ({ean.CharaCode})")) return false;
            }

            if(Files.AfterBacFile != null)
            {
                if (!Validate_DuplicateEntries(window, Files.AfterBacFile?.File?.BacEntries, "AFTER BAC")) return false;
            }

            return true;
        }

        private bool Validate_DuplicateEntries<T>(MetroWindow window, IList<T> entries, string typeText) where T : class, IInstallable
        {
            if (entries == null) return true;
            var conflicts = entries.Where(x => entries.Any(y => y.SortID == x.SortID && y != x));

            if(conflicts.Count() > 0)
            {
                window.ShowMessageAsync($"ID Conflict ({MoveType}: {Name})", $"There are multiple {typeText} entries with the ID {conflicts.First().SortID}. To continue with saving, please give each entry a unique ID!", MessageDialogStyle.Affirmative, DialogSettings.Default);
                return false;
            }
            return true;
        }

        private bool Validate_DuplicateArg0<T>(MetroWindow window, IList<Xv2File<T>> files, string valueText, string fileText) where T : class
        {
            if (files == null) return true;

            foreach(var file in files)
            {
                //Check for same chara code and language
                if(files.Any(x => x.CharaCode == file.CharaCode && x.IsEnglish == file.IsEnglish && x != file))
                {
                    window.ShowMessageAsync($"Duplicate {valueText}", $"There are multiple {fileText} with the same {valueText}. To continue with saving, please give each {fileText} a unique {valueText}.", MessageDialogStyle.Affirmative, DialogSettings.Default);
                    return false;
                }
            }

            return true;
        }
        #endregion

        /// <summary>
        /// Set name for all languages.
        /// </summary>
        public void SetName(string name)
        {
            Names = new string[(int)xv2.Language.NumLanguages];
            for (int i = 0; i < Names.Length; i++)
                Names[i] = name;
        }

        /// <summary>
        /// Set description for all languages.
        /// </summary>
        public void SetDesc(string desc)
        {
            Descs = new string[(int)xv2.Language.NumLanguages];
            for (int i = 0; i < Descs.Length; i++)
                Descs[i] = desc;
        }

        #region Helpers
        /// <summary>
        /// Returns the file instance associated with path, if it exists on this Move.
        /// </summary>
        /// <returns></returns>
        public object TryGetFileInstance(string path)
        {
            if (Files.BacFile?.Path == path) return Files.BacFile?.File;
            if (Files.BcmFile?.Path == path) return Files.BcmFile?.File;
            if (Files.BdmFile?.Path == path) return Files.BdmFile?.File;
            if (Files.ShotBdmFile?.Path == path) return Files.ShotBdmFile?.File;
            if (Files.EepkFile?.Path == path) return Files.EepkFile?.File;
            if (Files.BsaFile?.Path == path) return Files.BsaFile?.File;
            if (Files.AfterBacFile?.Path == path) return Files.AfterBacFile?.File;
            if (Files.AfterBcmFile?.Path == path) return Files.AfterBcmFile?.File;

            foreach (var acbFile in Files.SeAcbFile)
            {
                if (acbFile.Path == path) return acbFile.File;
            }

            foreach (var acbFile in Files.VoxAcbFile)
            {
                if (acbFile.Path == path) return acbFile.File;
            }

            foreach (var eanFile in Files.EanFile)
            {
                if (eanFile.Path == path) return eanFile.File;
            }

            foreach (var eanFile in Files.CamEanFile)
            {
                if (eanFile.Path == path) return eanFile.File;
            }

            return null;
        }

        //Try Gets
        /// <summary>
        /// Try to get the specified animation, if it belongs to this Move.
        /// </summary>
        /// <returns></returns>
        public EAN_Animation TryGetAnimation(BAC_Type0.EanType eanType, ushort eanIndex)
        {
            if (!BacType0_IsEanTypeSelfReference(MoveType, eanType)) return null;
            var ean = Editor.Files.Instance.GetEanFile(eanType, this, null, false, false);
            return ean?.Animations.FirstOrDefault(x => x.ID_UShort == eanIndex);
        }

        /// <summary>
        /// Try to get the specified hitbox (bdm entry), if it belongs to this Move.
        /// </summary>
        /// <returns></returns>
        public BDM_Entry TryGetHitbox(BAC_Type1.BdmType bdmType, ushort bdmId)
        {
            if (!BacType1_IsBdmTypeSelfReference(MoveType, bdmType)) return null;
            var file = Editor.Files.Instance.GetBdmFile(bdmType, this, null, false);
            return file?.BDM_Entries.FirstOrDefault(x => (ushort)x.ID == bdmId);
        }

        /// <summary>
        /// Try to get the specified effect, if it belongs to this Move.
        /// </summary>
        /// <returns></returns>
        public Xv2CoreLib.EEPK.Effect TryGetEffect(BAC_Type8.EepkTypeEnum eepkType, ushort skillId, ushort effectId)
        {
            if (!BacType8_IsEepkTypeSelfReference(MoveType, SkillID,eepkType, skillId)) return null;
            var file = Editor.Files.Instance.GetEepkFile(eepkType, skillId, this, null, false);
            return file?.Effects.FirstOrDefault(x => (ushort)x.SortID == effectId);
        }
        
        /// <summary>
        /// Try to get the specified projectile, if it belongs to this Move.
        /// </summary>
        /// <returns></returns>
        public BSA_Entry TryGetProjectile(BAC_Type9.BsaTypeEnum bsaType, ushort skillId, ushort bsaId)
        {
            if (!BacType9_IsBsaTypeSelfReference(MoveType, SkillID, bsaType, skillId)) return null;
            var file = Editor.Files.Instance.GetBsaFile(bsaType, skillId, this, null, false);
            return file?.BSA_Entries.FirstOrDefault(x => (ushort)x.SortID == bsaId);
        }

        /// <summary>
        /// Try to get the specified camera, if it belongs to this Move.
        /// </summary>
        /// <returns></returns>
        public EAN_Animation TryGetCamera(BAC_Type10.EanType eanType, ushort eanIndex)
        {
            if (!BacType10_IsEanTypeSelfReference(MoveType, eanType)) return null;
            var ean = Editor.Files.Instance.GetCamEanFile(eanType, this, null, false, false);
            return ean?.Animations.FirstOrDefault(x => x.ID_UShort == eanIndex);
        }

        /// <summary>
        /// Try to get the specified cue, if it belongs to this Move.
        /// </summary>
        /// <returns></returns>
        public Cue_Wrapper TryGetCue(Xv2CoreLib.BAC.AcbType acbType, ushort cueId)
        {
            if (!BacType11_IsAcbTypeSelfReference(MoveType, acbType)) return null;
            var acb = Editor.Files.Instance.GetAcbFile(acbType, this, null, false);
            return acb?.Cues.FirstOrDefault(x => x.CueRef.ID == cueId);
        }

        /// <summary>
        /// Try to get the specified ACB_Wrapper, if it belongs to this Move.
        /// </summary>
        /// <returns></returns>
        public ACB_Wrapper TryGetAcbFile(Xv2CoreLib.BAC.AcbType acbType)
        {
            if (!BacType11_IsAcbTypeSelfReference(MoveType, acbType)) return null;
            return Editor.Files.Instance.GetAcbFile(acbType, this, null, false);
        }


        //Self Reference Check
        public static bool BacType0_IsEanTypeSelfReference(Type MoveType, BAC_Type0.EanType eanType)
        {
            if (MoveType == Type.Moveset && eanType == BAC_Type0.EanType.Character) return true;
            if (MoveType == Type.Skill && eanType == BAC_Type0.EanType.Skill) return true;
            //if (MoveType == Type.CMN && eanType == BAC_Type0.EanType.Common) return true;
            return false;
        }

        public static bool BacType1_IsBdmTypeSelfReference(Type MoveType, BAC_Type1.BdmType bdmType)
        {
            if (MoveType == Type.Moveset && bdmType == BAC_Type1.BdmType.Character) return true;
            if (MoveType == Type.Skill && bdmType == BAC_Type1.BdmType.Skill) return true;
            //if (MoveType == Type.CMN && bdmType == BAC_Type1.BdmType.Common) return true;
            return false;
        }

        public static bool BacType8_IsEepkTypeSelfReference(Type MoveType, int SkillID, BAC_Type8.EepkTypeEnum eepkType, ushort skillId)
        {
            if (MoveType == Type.Skill && (eepkType == BAC_Type8.EepkTypeEnum.AwokenSkill || eepkType == BAC_Type8.EepkTypeEnum.SuperSkill || eepkType == BAC_Type8.EepkTypeEnum.UltimateSkill
                || eepkType == BAC_Type8.EepkTypeEnum.EvasiveSkill || eepkType == BAC_Type8.EepkTypeEnum.KiBlastSkill) && skillId == SkillID) return true;
            if (MoveType == Type.Moveset && eepkType == BAC_Type8.EepkTypeEnum.Character) return true;
            return false;
        }

        public static bool BacType9_IsBsaTypeSelfReference(Type MoveType, int SkillID, BAC_Type9.BsaTypeEnum bsaType, ushort bsaEntrySkillId)
        {
            if (MoveType == Type.Skill && (bsaType == BsaTypeEnum.AwokenSkill || bsaType == BsaTypeEnum.SuperSkill || bsaType == BsaTypeEnum.UltimateSkill
                || bsaType == BsaTypeEnum.EvasiveSkill || bsaType == BsaTypeEnum.KiBlastSkill) && bsaEntrySkillId == SkillID) return true;
            return false;
        }

        public static bool BacType10_IsEanTypeSelfReference(Type MoveType, BAC_Type10.EanType eanType)
        {
            if (MoveType == Type.Moveset && eanType == BAC_Type10.EanType.Character) return true;
            if (MoveType == Type.Skill && eanType == BAC_Type10.EanType.Skill) return true;
            return false;
        }

        public static bool BacType11_IsAcbTypeSelfReference(Type MoveType, Xv2CoreLib.BAC.AcbType acbType)
        {
            if (MoveType == Type.Skill && (acbType == Xv2CoreLib.BAC.AcbType.Skill_SE || acbType == Xv2CoreLib.BAC.AcbType.Skill_VOX)) return true;
            if (MoveType == Type.Moveset && (acbType == Xv2CoreLib.BAC.AcbType.Character_SE || acbType == Xv2CoreLib.BAC.AcbType.Character_VOX)) return true;
            return false;
        }
        
        //Convert
        /// <summary>
        /// Convert this Move to a Xv2Skill instance for use with Xv2CoreLib.Xenoverse2. 
        /// </summary>
        /// <returns></returns>
        public Xv2Skill ConvertToXv2Skill()
        {
            if (MoveType != Type.Skill) throw new InvalidOperationException("Move.ConvertToXv2Skill: Not a skill.");

            Xv2Skill skill = new Xv2Skill();
            skill.Files = Files;
            skill.PupEntries = PupEntries;
            skill.IdbEntry = IdbEntry;
            skill.CusEntry = CusEntry;
            skill.Name = Names;
            skill.Description = Descs;
            skill.BtlHud = AwokenStageNames;
            skill.skillType = SkillType;

            return skill;
        }

        public Xv2Character ConvertToXv2Character()
        {
            if (MoveType != Type.Moveset) throw new InvalidOperationException("Move.ConvertToXv2Character: Not a moveset.");

            Xv2Character chara = new Xv2Character();
            chara.MovesetFiles = Files;
            chara.CmsEntry = CmsEntry;

            return chara;
        }
        #endregion
    }
}
