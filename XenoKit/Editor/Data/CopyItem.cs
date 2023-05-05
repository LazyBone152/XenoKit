using System;
using System.Collections.Generic;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Helper;
using Xv2CoreLib;
using Xv2CoreLib.ACB;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BDM;
using Xv2CoreLib.BSA;
using Xv2CoreLib.EAN;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Editor
{
    public enum FileType
    {
        Bac,
        Bdm,
        ShotBdm,
        Bcm,
        Bsa
        //Acb, eepk, ean, cam copy/paste handled elsewhere
    }

    public enum EntryType
    {
        Main,
        Sub //Store as children of first entry
    }

    [Serializable]
    public class CopyItem
    {
        public FileType fileType;
        public EntryType entryType;
        public Move.Type MoveType;
        public int SkillID;

        CopyEntries Primary = new CopyEntries(); //Includes all entries directly copied
        CopyEntries Secondary = new CopyEntries(); //Includes all entries referenced in Primary (Optional)
        
        //ID References, used for changing IDs before pasting
        private List<ValueReference> ValueRefs = new List<ValueReference>();

        public CopyItem(IList<BAC_Entry> bacEntries, Move move)
        {
            fileType = FileType.Bac;
            entryType = EntryType.Main;
            MoveType = move.MoveType;
            SkillID = move.SkillID;

            //Copy references into Secondary
            foreach(var bacEntry in bacEntries)
            {
                CopyBacEntryReferences(bacEntry, move);
            }

            //Add bac entry to Primary
            Primary.BacEntries.AddRange(bacEntries);
            
            RemoveDuplicates();
        }

        public CopyItem(IList<IBacType> bacTypes, Move move)
        {
            fileType = FileType.Bac;
            entryType = EntryType.Sub;
            MoveType = move.MoveType;
            SkillID = move.SkillID;

            BAC_Entry temp = new BAC_Entry();
            temp.IBacTypes = new AsyncObservableCollection<IBacType>(bacTypes);

            //Copy references into Secondary
            foreach (var bacEntry in bacTypes)
            {
                CopyBacEntryReferences(temp, move);
            }

            //Add bac entry to Primary
            Primary.BacEntries.Add(temp);

            RemoveDuplicates();
        }


        #region Copy
        //BAC
        private void CopyBacEntryReferences(BAC_Entry bacEntry, Move move)
        {
            foreach (var bacType in bacEntry.IBacTypes)
            {
                if (bacType is BAC_Type0 type0)
                    CopyBacType0References(type0, move);
                else if (bacType is BAC_Type1 type1)
                    CopyBacType1References(type1, move);
                else if (bacType is BAC_Type8 type8)
                    CopyBacType8References(type8, move);
                else if (bacType is BAC_Type9 type9)
                    CopyBacType9References(type9, move);
                else if (bacType is BAC_Type10 type10)
                    CopyBacType10References(type10, move);
                else if (bacType is BAC_Type11 type11)
                    CopyBacType11References(type11, move);
                else if (bacType is BAC_Type17 type17)
                    CopyBacType17References(type17, move);
            }
        }

        private void CopyBacType0References(BAC_Type0 bacType, Move move)
        {
            if(bacType.EanIndex != ushort.MaxValue)
            {
                var animation = move.TryGetAnimation(bacType.Ean_Type, bacType.EanIndex);

                if (animation != null && !Secondary.Animations.Any(x => x.ID_UShort == animation?.ID_UShort))
                    Secondary.Animations.Add(animation);

                if(animation != null)
                {
                    ValueRefs.Add(new ValueReference(bacType, nameof(bacType.EanIndex), ValueReference.InstanceRefType.Ean));
                    ValueRefs.Add(new ValueReference(bacType, nameof(bacType.Ean_Type), ValueReference.InstanceRefType.Ean, ValueReference.Mode.Type));
                }
            }
        }

        private void CopyBacType1References(BAC_Type1 bacType, Move move)
        {
            if (CopyHitbox(bacType.bdmFile, bacType.BdmEntryID, move))
            {
                ValueRefs.Add(new ValueReference(bacType, nameof(bacType.BdmEntryID), ValueReference.InstanceRefType.Bdm));
                ValueRefs.Add(new ValueReference(bacType, nameof(bacType.bdmFile), ValueReference.InstanceRefType.Bdm, ValueReference.Mode.Type));
            }
        }

        private void CopyBacType8References(BAC_Type8 bacType, Move move)
        {
            if(CopyEffect(bacType.EepkType, bacType.EffectID, bacType.SkillID, move))
            {
                ValueRefs.Add(new ValueReference(bacType, nameof(bacType.EffectID), ValueReference.InstanceRefType.Eepk));
                ValueRefs.Add(new ValueReference(bacType, nameof(bacType.EepkType), ValueReference.InstanceRefType.Eepk, ValueReference.Mode.Type));
                ValueRefs.Add(new ValueReference(bacType, nameof(bacType.SkillID), ValueReference.InstanceRefType.Eepk, ValueReference.Mode.SkillId));
            }

        }

        private void CopyBacType9References(BAC_Type9 bacType, Move move)
        {
            if(CopyProjectile(bacType.BsaType, bacType.EntryID, bacType.SkillID, move))
            {
                ValueRefs.Add(new ValueReference(bacType, nameof(bacType.EntryID), ValueReference.InstanceRefType.Bsa));
                ValueRefs.Add(new ValueReference(bacType, nameof(bacType.BsaType), ValueReference.InstanceRefType.Bsa, ValueReference.Mode.Type));
                ValueRefs.Add(new ValueReference(bacType, nameof(bacType.SkillID), ValueReference.InstanceRefType.Bsa, ValueReference.Mode.SkillId));
            }
        }

        private void CopyBacType10References(BAC_Type10 bacType, Move move)
        {
            if (bacType.EanIndex != ushort.MaxValue)
            {
                var camera = move.TryGetCamera(bacType.Ean_Type, bacType.EanIndex);

                if (camera != null && !Secondary.Cameras.Any(x => x.ID_UShort == camera?.ID_UShort))
                    Secondary.Cameras.Add(camera);

                if(camera != null)
                {
                    ValueRefs.Add(new ValueReference(bacType, nameof(bacType.EanIndex), ValueReference.InstanceRefType.Cam));
                    ValueRefs.Add(new ValueReference(bacType, nameof(bacType.Ean_Type), ValueReference.InstanceRefType.Cam, ValueReference.Mode.Type));
                }
            }
        }

        private void CopyBacType11References(BAC_Type11 bacType, Move move)
        {
            if(CopyCue(bacType.AcbType, bacType.CueId, move))
            {
                ValueRefs.Add(new ValueReference(bacType, nameof(bacType.CueId), ValueReference.InstanceRefType.SeAcb));
                ValueRefs.Add(new ValueReference(bacType, nameof(bacType.AcbType), ValueReference.InstanceRefType.SeAcb, ValueReference.Mode.Type));
            }
        }

        private void CopyBacType17References(BAC_Type17 bacType, Move move)
        {
            if(CopyBacEntry(bacType.BacEntryId, move))
                ValueRefs.Add(new ValueReference(bacType, nameof(bacType.BacEntryId), ValueReference.InstanceRefType.Bac));
        }

        //BDM
        private void CopyBdmEntryReferences(BDM_Entry bdmEntry, Move move, bool shotBdm = false)
        {
            foreach (var subEntry in bdmEntry.Type0Entries)
            {
                if(CopyCue((Xv2CoreLib.BAC.AcbType)subEntry.AcbType, (ushort)subEntry.CueId, move))
                {
                    ValueRefs.Add(new ValueReference(subEntry, nameof(subEntry.CueId), ValueReference.InstanceRefType.SeAcb));
                    ValueRefs.Add(new ValueReference(subEntry, nameof(subEntry.AcbType), ValueReference.InstanceRefType.SeAcb, ValueReference.Mode.Type));
                }

                if(CopyEffect((BAC_Type8.EepkTypeEnum)subEntry.Effect1_EepkType, subEntry.Effect1_ID, subEntry.Effect1_SkillID, move))
                {
                    ValueRefs.Add(new ValueReference(subEntry, nameof(subEntry.Effect1_ID), ValueReference.InstanceRefType.Eepk));
                    ValueRefs.Add(new ValueReference(subEntry, nameof(subEntry.Effect1_EepkType), ValueReference.InstanceRefType.Eepk, ValueReference.Mode.Type));
                    ValueRefs.Add(new ValueReference(subEntry, nameof(subEntry.Effect1_SkillID), ValueReference.InstanceRefType.Eepk, ValueReference.Mode.SkillId));
                }

                if(CopyEffect((BAC_Type8.EepkTypeEnum)subEntry.Effect2_EepkType, subEntry.Effect2_ID, subEntry.Effect2_SkillID, move))
                {
                    ValueRefs.Add(new ValueReference(subEntry, nameof(subEntry.Effect2_ID), ValueReference.InstanceRefType.Eepk));
                    ValueRefs.Add(new ValueReference(subEntry, nameof(subEntry.Effect2_EepkType), ValueReference.InstanceRefType.Eepk, ValueReference.Mode.Type));
                    ValueRefs.Add(new ValueReference(subEntry, nameof(subEntry.Effect2_SkillID), ValueReference.InstanceRefType.Eepk, ValueReference.Mode.SkillId));
                }

                if (CopyEffect((BAC_Type8.EepkTypeEnum)subEntry.Effect3_EepkType, subEntry.Effect3_ID, subEntry.Effect3_SkillID, move))
                {
                    ValueRefs.Add(new ValueReference(subEntry, nameof(subEntry.Effect3_ID), ValueReference.InstanceRefType.Eepk));
                    ValueRefs.Add(new ValueReference(subEntry, nameof(subEntry.Effect3_EepkType), ValueReference.InstanceRefType.Eepk, ValueReference.Mode.Type));
                    ValueRefs.Add(new ValueReference(subEntry, nameof(subEntry.Effect3_SkillID), ValueReference.InstanceRefType.Eepk, ValueReference.Mode.SkillId));
                }

                if (shotBdm)
                {
                    if(CopyShotHitbox((ushort)subEntry.StaminaBrokenOverrideBdmId, move))
                        ValueRefs.Add(new ValueReference(subEntry, nameof(subEntry.StaminaBrokenOverrideBdmId), ValueReference.InstanceRefType.ShotBdm));
                }
                else if ((MoveType == Move.Type.Skill))
                {
                    if (CopyHitbox(BAC_Type1.BdmType.Skill, (ushort)subEntry.StaminaBrokenOverrideBdmId, move))
                        ValueRefs.Add(new ValueReference(subEntry, nameof(subEntry.StaminaBrokenOverrideBdmId), ValueReference.InstanceRefType.Bdm));
                }
                else if ((MoveType == Move.Type.Moveset))
                {
                    if (CopyHitbox(BAC_Type1.BdmType.Character, (ushort)subEntry.StaminaBrokenOverrideBdmId, move))
                        ValueRefs.Add(new ValueReference(subEntry, nameof(subEntry.StaminaBrokenOverrideBdmId), ValueReference.InstanceRefType.Bdm));
                }

            }
        }

        //BSA
        private void CopyBsaEntryReferences(BSA_Entry bsaEntry, Move move)
        {
            //Passing
            if(CopySelfProjectile(bsaEntry.Expires, move))
                ValueRefs.Add(new ValueReference(bsaEntry, nameof(bsaEntry.Expires), ValueReference.InstanceRefType.Bsa));

            if (CopySelfProjectile(bsaEntry.ImpactEnemy, move))
                ValueRefs.Add(new ValueReference(bsaEntry, nameof(bsaEntry.ImpactEnemy), ValueReference.InstanceRefType.Bsa));

            if (CopySelfProjectile(bsaEntry.ImpactGround, move))
                ValueRefs.Add(new ValueReference(bsaEntry, nameof(bsaEntry.ImpactGround), ValueReference.InstanceRefType.Bsa));

            if (CopySelfProjectile(bsaEntry.ImpactProjectile, move))
                ValueRefs.Add(new ValueReference(bsaEntry, nameof(bsaEntry.ImpactProjectile), ValueReference.InstanceRefType.Bsa));

            //Collision
            foreach (var unk1 in bsaEntry.SubEntries.CollisionEntries)
            {
                if(CopyEffect((BAC_Type8.EepkTypeEnum)unk1.EepkType, unk1.EffectID, unk1.SkillID, move))
                {
                    ValueRefs.Add(new ValueReference(unk1, nameof(unk1.EffectID), ValueReference.InstanceRefType.Eepk));
                    ValueRefs.Add(new ValueReference(unk1, nameof(unk1.EepkType), ValueReference.InstanceRefType.Eepk, ValueReference.Mode.Type));
                    ValueRefs.Add(new ValueReference(unk1, nameof(unk1.SkillID), ValueReference.InstanceRefType.Eepk, ValueReference.Mode.SkillId));
                }
            }

            //Types
            foreach(var bsaType in bsaEntry.IBsaTypes)
            {
                if (bsaType is BSA_Type0 type0)
                    CopyBsaType0References(type0, move);
                else if (bsaType is BSA_Type3 type3)
                    CopyBsaType3References(type3, move);
                else if (bsaType is BSA_Type6 type6)
                    CopyBsaType6References(type6, move);
                else if (bsaType is BSA_Type7 type7)
                    CopyBsaType7References(type7, move);
            }
        }

        private void CopyBsaType0References(BSA_Type0 bsaType, Move move)
        {
            if (bsaType.BSA_EntryID == ushort.MaxValue) return;

            var entry = move.Files.BsaFile.File.BSA_Entries.FirstOrDefault(x => x.SortID == bsaType.BSA_EntryID);

            if (entry != null && !Secondary.BsaEntries.Any(x => (ushort)x.SortID == bsaType.BSA_EntryID))
            {
                Secondary.BsaEntries.Add(entry);
                CopyBsaEntryReferences(entry, move);
            }

            if(entry != null)
                ValueRefs.Add(new ValueReference(bsaType, nameof(bsaType.BSA_EntryID), ValueReference.InstanceRefType.Bsa));
        }

        private void CopyBsaType3References(BSA_Type3 bsaType, Move move)
        {
            if(CopyShotHitbox(bsaType.FirstHit, move))
                ValueRefs.Add(new ValueReference(bsaType, nameof(bsaType.FirstHit), ValueReference.InstanceRefType.ShotBdm));

            if(CopyShotHitbox(bsaType.MultipleHits, move))
                ValueRefs.Add(new ValueReference(bsaType, nameof(bsaType.MultipleHits), ValueReference.InstanceRefType.ShotBdm));

            if(CopyShotHitbox(bsaType.LastHit, move))
                ValueRefs.Add(new ValueReference(bsaType, nameof(bsaType.LastHit), ValueReference.InstanceRefType.ShotBdm));
        }

        private void CopyBsaType6References(BSA_Type6 bsaType, Move move)
        {
            if(CopyEffect((BAC_Type8.EepkTypeEnum)bsaType.EepkType, bsaType.EffectID, bsaType.SkillID, move))
            {
                ValueRefs.Add(new ValueReference(bsaType, nameof(bsaType.EffectID), ValueReference.InstanceRefType.Eepk));
                ValueRefs.Add(new ValueReference(bsaType, nameof(bsaType.EepkType), ValueReference.InstanceRefType.Eepk, ValueReference.Mode.Type));
                ValueRefs.Add(new ValueReference(bsaType, nameof(bsaType.SkillID), ValueReference.InstanceRefType.Eepk, ValueReference.Mode.SkillId));
            }
        }

        private void CopyBsaType7References(BSA_Type7 bsaType, Move move)
        {
            if(CopyCue((Xv2CoreLib.BAC.AcbType)bsaType.AcbType, bsaType.CueId, move))
            {
                ValueRefs.Add(new ValueReference(bsaType, nameof(bsaType.CueId), ValueReference.InstanceRefType.SeAcb));
                ValueRefs.Add(new ValueReference(bsaType, nameof(bsaType.AcbType), ValueReference.InstanceRefType.SeAcb, ValueReference.Mode.Type));
            }
        }
        #endregion
        
        #region CopyGeneral
        private bool CopyBacEntry(int bacId, Move move)
        {
            if(bacId != -1 && bacId != ushort.MaxValue)
            {
                var bacEntry = move.Files.BacFile.File.GetEntry(bacId);

                if(bacEntry != null && !Secondary.BacEntries.Any(x => x.SortID == bacId))
                {
                    Secondary.BacEntries.Add(bacEntry);
                    CopyBacEntryReferences(bacEntry, move);
                    return true;
                }
            }
            return false;
        }

        private bool CopyHitbox(BAC_Type1.BdmType bdmType, ushort bdmId, Move move)
        {
            if (bdmId != ushort.MaxValue)
            {
                var hitbox = move.TryGetHitbox(bdmType, bdmId);
                
                if (hitbox != null && !Secondary.BdmEntries.Any(x => (ushort)x.ID == bdmId))
                {
                    Secondary.BdmEntries.Add(hitbox);
                    CopyBdmEntryReferences(hitbox, move);
                }

                return hitbox != null;
            }
            return false;
        }

        private bool CopyShotHitbox(ushort bdmId, Move move)
        {
            if (bdmId != ushort.MaxValue)
            {
                var hitbox = move.Files.ShotBdmFile.File.BDM_Entries.FirstOrDefault(x => x.ID == bdmId);

                if (hitbox != null && !Secondary.ShotBdmEntries.Any(x => (ushort)x.ID == bdmId))
                {
                    Secondary.ShotBdmEntries.Add(hitbox);
                    CopyBdmEntryReferences(hitbox, move, true);
                }
                return hitbox != null;
            }
            return false;
        }
        
        private bool CopyProjectile(BAC_Type9.BsaTypeEnum bsaType, int bsaId, ushort skillId, Move move)
        {
            if (bsaId != -1)
            {
                var projectile = move.TryGetProjectile(bsaType, skillId, (ushort)bsaId);

                if (projectile != null && !Secondary.BsaEntries.Any(x => (ushort)x.SortID == bsaId))
                {
                    Secondary.BsaEntries.Add(projectile);
                    CopyBsaEntryReferences(projectile, move);
                }

                return projectile != null;
            }
            return false;
        }

        private bool CopySelfProjectile(int bsaId, Move move)
        {
            if (bsaId != -1)
            {
                var projectile = move.Files.BsaFile.File.BSA_Entries.FirstOrDefault(x => x.SortID == bsaId);

                if (projectile != null && !Secondary.BsaEntries.Any(x => (ushort)x.SortID == bsaId))
                {
                    Secondary.BsaEntries.Add(projectile);
                    CopyBsaEntryReferences(projectile, move);
                }

                return projectile != null;
            }
            return false;
        }

        private bool CopyCue(Xv2CoreLib.BAC.AcbType acbType, ushort cueId, Move move)
        {
            if (cueId != ushort.MaxValue && (acbType == Xv2CoreLib.BAC.AcbType.Character_SE || acbType == Xv2CoreLib.BAC.AcbType.Skill_SE))
            {
                //Only copy SE sounds
                var acbFile = move.TryGetAcbFile(acbType);

                if (acbFile != null && !Secondary.SeAcbFile.Cues.Any(x => x.ID == cueId))
                    Secondary.SeAcbFile.CopyCue(cueId, acbFile.AcbFile);

                return acbFile != null;
            }
            return false;
        }

        private bool CopyEffect(BAC_Type8.EepkTypeEnum eepkType, int effectId, ushort skillId, Move move)
        {
            if (effectId != -1 && effectId != ushort.MaxValue)
            {
                var effect = move.TryGetEffect(eepkType, skillId, (ushort)effectId);

                if (effect != null && !Secondary.Effects.Any(x => (ushort)x.SortID == effectId))
                    Secondary.Effects.Add(effect);

                return effect != null;
            }
            return false;
        }
        #endregion

        #region Paste
        public List<IUndoRedo> PasteIntoMove_Sub(BAC_Entry mainEntry, Move move, bool copyReferences)
        {
            if (entryType != EntryType.Sub)
                throw new InvalidOperationException($"{nameof(CopyItem)}.{nameof(PasteIntoMove_Sub)}: function can only be called with entryType = Sub!");

            RemoveDuplicates();
            List<IUndoRedo> undos = new List<IUndoRedo>();
            
            undos.AddRange(PasteReferences(move, copyReferences));
            
            foreach(var bacEntry in Primary.BacEntries[0].IBacTypes)
            {
                mainEntry.IBacTypes.Add(bacEntry);
                undos.Add(new UndoableListAdd<IBacType>(mainEntry.IBacTypes, bacEntry));
            }

            return undos;
        }

        public List<IUndoRedo> PasteIntoMove_Main(Move move, bool copyReferences)
        {
            if (entryType != EntryType.Main)
                throw new InvalidOperationException($"{nameof(CopyItem)}.{nameof(PasteIntoMove_Main)}: function can only be called with entryType = Main!");

            RemoveDuplicates();
            List<IUndoRedo> undos = new List<IUndoRedo>();

            undos.AddRange(PasteReferences(move, copyReferences));
            undos.AddRange(PasteEntries(move, Primary));

            return undos;
        }

        private List<IUndoRedo> PasteReferences(Move move, bool copyReferences)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if (copyReferences)
            {
                //Update Type and SkillID values on all copied references to point to new Move
                ReplaceTypeAndSkillIdReferences(move, ValueReference.InstanceRefType.Bdm);
                ReplaceTypeAndSkillIdReferences(move, ValueReference.InstanceRefType.Eepk);
                ReplaceTypeAndSkillIdReferences(move, ValueReference.InstanceRefType.SeAcb);
                ReplaceTypeAndSkillIdReferences(move, ValueReference.InstanceRefType.Bsa);
                ReplaceTypeAndSkillIdReferences(move, ValueReference.InstanceRefType.Ean);
                ReplaceTypeAndSkillIdReferences(move, ValueReference.InstanceRefType.Cam);
            }
            
            if (copyReferences)
                undos.AddRange(PasteEntries(move, Secondary));

            return undos;
        }

        private List<IUndoRedo> PasteEntries(Move move, CopyEntries entries)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            //BAC
            PasteBacEntries(entries.BacEntries, move);

            //BDM
            PasteBdmEntries(entries.BdmEntries, move);

            //SHOT.BDM
            PasteShotBdmEntries(entries.ShotBdmEntries, move);

            //BSA
            PasteBsaEntries(entries.BsaEntries, move);

            //EEPK
            foreach (var effect in entries.Effects)
            {
                int oldId = effect.SortID;
                int newId = move.Files.EepkFile.File.GetUnusedEffectId(100);
                effect.SortID = newId;
                undos.AddRange(move.Files.EepkFile.File.AddEffect(effect, true));
                ReplaceIdReference(ValueReference.InstanceRefType.Eepk, oldId, newId);
            }

            //SE ACB
            foreach (var cue in entries.SeAcbFile.Cues)
            {
                int oldId = (int)cue.ID;
                int newId = GeneralHelpers.AssignCommonCueId(move.Files.SeAcbFile);
                cue.ID = (uint)newId;

                foreach(var acb in move.Files.SeAcbFile)
                    undos.AddRange(acb.File.AcbFile.CopyCue(oldId, entries.SeAcbFile));

                ReplaceIdReference(ValueReference.InstanceRefType.SeAcb, oldId, newId);
            }

            //EAN
            foreach (var animation in entries.Animations)
            {
                var ean = move.Files.GetDefaultOrFirstEanFile();

                if(ean != null)
                {
                    int oldId = animation.SortID;
                    int newId = ean.AddEntry(animation);
                    ReplaceIdReference(ValueReference.InstanceRefType.Ean, oldId, newId);
                    undos.Add(new UndoableListAdd<EAN_Animation>(ean.Animations, animation));
                }
                else
                {
                    Log.Add($"PasteEntries: no ean found to paste animation \"{animation.Name}\" into.", LogType.Warning);
                }

                ean.LinkEskData();
            }

            //CAM
            foreach (var camera in entries.Cameras)
            {
                var ean = move.Files.GetDefaultOrFirstCamEanFile();

                if (ean != null)
                {
                    int oldId = camera.SortID;
                    int newId = ean.AddEntry(camera);
                    ReplaceIdReference(ValueReference.InstanceRefType.Cam, oldId, newId);
                    undos.Add(new UndoableListAdd<EAN_Animation>(ean.Animations, camera));
                }
                else
                {
                    Log.Add($"PasteEntries: no cam.ean found to paste camera \"{camera.Name}\" into.", LogType.Warning);
                }
            }

            return undos;
        }

        private List<IUndoRedo> PasteBacEntries(IList<BAC_Entry> bacEntries, Move move)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var bacEntry in bacEntries)
            {
                int oldId = bacEntry.SortID;
                int newId = move.Files.BacFile.File.AddEntry(bacEntry);
                ReplaceIdReference(ValueReference.InstanceRefType.Bac, oldId, newId);
                undos.Add(new UndoableListAdd<BAC_Entry>(move.Files.BacFile.File.BacEntries, bacEntry));
            }

            return undos;
        }
        
        private List<IUndoRedo> PasteBdmEntries(IList<BDM_Entry> bdmEntries, Move move)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var bdmEntry in bdmEntries)
            {
                int oldId = bdmEntry.SortID;
                int newId = move.Files.BdmFile.File.AddEntry(bdmEntry);
                ReplaceIdReference(ValueReference.InstanceRefType.Bdm, oldId, newId);
                undos.Add(new UndoableListAdd<BDM_Entry>(move.Files.BdmFile.File.BDM_Entries, bdmEntry));
            }

            return undos;
        }

        private List<IUndoRedo> PasteShotBdmEntries(IList<BDM_Entry> bdmEntries, Move move)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var bdmEntry in bdmEntries)
            {
                int oldId = bdmEntry.SortID;
                int newId = move.Files.ShotBdmFile.File.AddEntry(bdmEntry);
                ReplaceIdReference(ValueReference.InstanceRefType.ShotBdm, oldId, newId);
                undos.Add(new UndoableListAdd<BDM_Entry>(move.Files.ShotBdmFile.File.BDM_Entries, bdmEntry));
            }

            return undos;
        }

        private List<IUndoRedo> PasteBsaEntries(IList<BSA_Entry> bsaEntries, Move move)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var bsaEntry in bsaEntries)
            {
                int oldId = bsaEntry.SortID;
                int newId = move.Files.BsaFile.File.AddEntry(bsaEntry);
                ReplaceIdReference(ValueReference.InstanceRefType.Bsa, oldId, newId);
                undos.Add(new UndoableListAdd<BSA_Entry>(move.Files.BsaFile.File.BSA_Entries, bsaEntry));
            }

            return undos;
        }

        #endregion

        #region Utils
        private void RemoveDuplicates()
        {
            //Remove any entries in Secondary that already exist in Primary.

            RemoveDuplicates(Primary.BacEntries, Secondary.BacEntries);
            RemoveDuplicates(Primary.BdmEntries, Secondary.BdmEntries);
            RemoveDuplicates(Primary.ShotBdmEntries, Secondary.ShotBdmEntries);
            RemoveDuplicates(Primary.BsaEntries, Secondary.BsaEntries);
            //Animations, cameras, effects and cues cannot be in primary, so there are no duplicates to remove
        }

        private void RemoveDuplicates<T>(IList<T> primaryEntries, IList<T> secondaryEntries) where T : IInstallable
        {
            foreach(var entry in primaryEntries)
            {
                var existing = secondaryEntries.FirstOrDefault(x => x.SortID == entry.SortID);

                if (existing != null)
                {
                    secondaryEntries.Remove(existing);
                }
            }
        }

        private void ReplaceIdReference(ValueReference.InstanceRefType refType, int oldId, int newId)
        {
            var refs = ValueRefs.Where(x => x.oldId == oldId && x.RefType == refType);

            foreach(var idRef in refs)
            {
                idRef.ReplaceValue(newId);
            }

            ValueRefs.RemoveAll(x => refs.Contains(x));
        }

        private void ReplaceTypeAndSkillIdReferences(Move move, ValueReference.InstanceRefType refType)
        {
            var refs = ValueRefs.Where(x => x.RefType == refType && x.mode == ValueReference.Mode.SkillId);

            //Skill ID
            foreach (var valueRef in refs)
            {
                if(move.MoveType == Move.Type.Skill)
                    valueRef.ReplaceValue(move.SkillID);
                else if (move.MoveType == Move.Type.Moveset)
                    valueRef.ReplaceValue(-1);
                
            }

            //Remove skill ID references
            ValueRefs.RemoveAll(x => refs.Contains(x));
            
            refs = ValueRefs.Where(x => x.RefType == refType && x.mode == ValueReference.Mode.Type);

            //Type
            foreach (var valueRef in refs)
            {
                if(move.MoveType == Move.Type.Skill)
                {
                    if (refType == ValueReference.InstanceRefType.Bdm)
                        valueRef.SetEnum((int)BAC_Type1.BdmType.Skill);
                    else if (refType == ValueReference.InstanceRefType.Bsa)
                        valueRef.SetEnum(move.NumericSkillType);
                    else if (refType == ValueReference.InstanceRefType.Eepk)
                        valueRef.SetEnum(move.NumericSkillType);
                    else if (refType == ValueReference.InstanceRefType.SeAcb)
                        valueRef.SetEnum((int)Xv2CoreLib.BAC.AcbType.Skill_SE);
                    else if (refType == ValueReference.InstanceRefType.Ean)
                        valueRef.SetEnum((int)BAC_Type0.EanType.Skill);
                    else if (refType == ValueReference.InstanceRefType.Cam)
                        valueRef.SetEnum((int)BAC_Type10.EanType.Skill);
                }
                else if(move.MoveType == Move.Type.Moveset)
                {
                    if (refType == ValueReference.InstanceRefType.Bdm)
                        valueRef.SetEnum((int)BAC_Type1.BdmType.Character);
                    else if (refType == ValueReference.InstanceRefType.Eepk)
                        valueRef.SetEnum(move.NumericSkillType);
                    else if (refType == ValueReference.InstanceRefType.SeAcb)
                        valueRef.SetEnum((int)Xv2CoreLib.BAC.AcbType.Character_SE);
                    else if (refType == ValueReference.InstanceRefType.Ean)
                        valueRef.SetEnum((int)BAC_Type0.EanType.Character);
                    else if (refType == ValueReference.InstanceRefType.Cam)
                        valueRef.SetEnum((int)BAC_Type10.EanType.Character);
                }
                
            }
            
            //Remove Type references
            ValueRefs.RemoveAll(x => refs.Contains(x));
        }
        
        #endregion

        #region Helpers
        public int NumMainEntries()
        {
            int total = 0;
            total += Primary.BacEntries.Count;
            total += Primary.BdmEntries.Count;
            total += Primary.ShotBdmEntries.Count;
            total += Primary.BsaEntries.Count;
            return total;
        }

        public int NumReferences()
        {
            int total = 0;
            total += Secondary.BacEntries.Count;
            total += Secondary.BdmEntries.Count;
            total += Secondary.ShotBdmEntries.Count;
            total += Secondary.BsaEntries.Count;
            total += Secondary.Animations.Count;
            total += Secondary.Cameras.Count;
            total += Secondary.SeAcbFile.Cues.Count;
            total += Secondary.Effects.Count;
            return total;
        }

        public string MainEntriesDetails()
        {
            if(entryType == EntryType.Main)
            {
                if (fileType == FileType.Bac)
                    return $"{Primary.BacEntries.Count} Bac Entries";
                else if (fileType == FileType.Bsa)
                    return $"{Primary.BsaEntries.Count} Bsa Entries";
                else if (fileType == FileType.Bdm)
                    return $"{Primary.BdmEntries.Count} Bdm Entries";
                else if (fileType == FileType.ShotBdm)
                    return $"{Primary.ShotBdmEntries.Count} Shot.Bdm Entries";
            }
            else if(entryType == EntryType.Sub)
            {
                try
                {
                    if (fileType == FileType.Bac)
                        return $"{Primary.BacEntries[0].IBacTypes.Count} Bac Types";
                    else if (fileType == FileType.Bsa)
                        return $"{Primary.BsaEntries[0].IBsaTypes.Count} Bsa Types";
                    else if (fileType == FileType.Bdm)
                        return $"{Primary.BdmEntries[0].Type0Entries.Count} Bdm SubEntries";
                    else if (fileType == FileType.ShotBdm)
                        return $"{Primary.ShotBdmEntries[0].Type0Entries.Count} Shot.Bdm SubEntries";
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        public string ReferencesDetails()
        {
            return $"Bac Entries: {Secondary.BacEntries.Count}\n" +
                $"Bdm Entries: {Secondary.BdmEntries.Count}\n" +
                $"Bsa Entries: {Secondary.BsaEntries.Count}\n" +
                $"Shot.Bdm Entries: {Secondary.ShotBdmEntries.Count}\n" +
                $"Animations: {Secondary.Animations.Count}\n" +
                $"Cameras: {Secondary.Cameras.Count}\n" +
                $"SE Cues: {Secondary.SeAcbFile.Cues.Count}\n" +
                $"Effects: {Secondary.Effects.Count}\n";
        }
        #endregion
    }

    [Serializable]
    public class CopyEntries
    {
        public List<BAC_Entry> BacEntries { get; set; } = new List<BAC_Entry>();
        public List<BDM_Entry> BdmEntries { get; set; } = new List<BDM_Entry>();
        public List<BDM_Entry> ShotBdmEntries { get; set; } = new List<BDM_Entry>();
        public List<BSA_Entry> BsaEntries { get; set; } = new List<BSA_Entry>();
        public List<EAN_Animation> Animations { get; set; } = new List<EAN_Animation>();
        public List<EAN_Animation> Cameras { get; set; } = new List<EAN_Animation>();
        public List<Effect> Effects { get; set; } = new List<Effect>();
        public ACB_File SeAcbFile { get; set; } = ACB_File.NewXv2Acb();
        

    }

    [Serializable]
    struct ReplacedId
    {
        public object IdObject { get; set; }
        public int IdIndex { get; set; } //For use when there are multiple references on an object (such as BDM Entries whcib have an acb, 3 effects, another bdm entry...)

        //BDM:
        //0 = ACB, 1 = Effect1, 2 = Effect2, 3 = Effect3, 4 = StaminaBrokenOverrideBdmId

        public ReplacedId(object idObect)
        {
            IdObject = idObect;
            IdIndex = 0;
        }

        public ReplacedId(object idObect, int idIndex)
        {
            IdObject = idObect;
            IdIndex = idIndex;
        }
    }

    [Serializable]
    class ValueReference
    {
        internal enum Mode
        {
            Id,
            Type,
            SkillId
        }

        internal enum InstanceRefType
        {
            Bac,
            Bcm, 
            Bdm,
            ShotBdm,
            Bsa,
            SeAcb,
            Ean,
            Cam,
            Eepk
        }
        
        public object Instance;
        public string propName;
        public InstanceRefType RefType;
        public Mode mode;

        //Mode specific:
        public int oldId;

        public ValueReference(object instance, string _propName, InstanceRefType refType, Mode _mode = Mode.Id)
        {
            Instance = instance;
            propName = _propName;
            object numObj = Instance.GetType().GetProperty(propName).GetValue(Instance);
            oldId = Convert.ToInt32(numObj);
            RefType = refType;
            mode = _mode;
            
        }

        public void SetEnum(int newValue)
        {
            if (Instance.GetType().GetProperty(propName).PropertyType == typeof(BAC_Type1.BdmType))
                Instance.GetType().GetProperty(propName).SetValue(Instance, (BAC_Type1.BdmType)newValue, null);
            else if (Instance.GetType().GetProperty(propName).PropertyType == typeof(BAC_Type0.EanType))
                Instance.GetType().GetProperty(propName).SetValue(Instance, (BAC_Type0.EanType)newValue, null);
            else if (Instance.GetType().GetProperty(propName).PropertyType == typeof(BAC_Type8.EepkTypeEnum))
                Instance.GetType().GetProperty(propName).SetValue(Instance, (BAC_Type8.EepkTypeEnum)newValue, null);
            else if (Instance.GetType().GetProperty(propName).PropertyType == typeof(BAC_Type9.BsaTypeEnum))
                Instance.GetType().GetProperty(propName).SetValue(Instance, (BAC_Type9.BsaTypeEnum)newValue, null);
            else if (Instance.GetType().GetProperty(propName).PropertyType == typeof(BAC_Type10.EanType))
                Instance.GetType().GetProperty(propName).SetValue(Instance, (BAC_Type10.EanType)newValue, null);
            else if (Instance.GetType().GetProperty(propName).PropertyType == typeof(Xv2CoreLib.BAC.AcbType))
                Instance.GetType().GetProperty(propName).SetValue(Instance, (Xv2CoreLib.BAC.AcbType)newValue, null);
            else if (Instance.GetType().GetProperty(propName).PropertyType == typeof(Xv2CoreLib.BDM.AcbType))
                Instance.GetType().GetProperty(propName).SetValue(Instance, (Xv2CoreLib.BDM.AcbType)newValue, null);
            else if (Instance.GetType().GetProperty(propName).PropertyType == typeof(Xv2CoreLib.BDM.EepkType))
                Instance.GetType().GetProperty(propName).SetValue(Instance, (Xv2CoreLib.BDM.EepkType)newValue, null);
            else if (Instance.GetType().GetProperty(propName).PropertyType == typeof(Xv2CoreLib.BSA.AcbType))
                Instance.GetType().GetProperty(propName).SetValue(Instance, (Xv2CoreLib.BSA.AcbType)newValue, null);
            else if (Instance.GetType().GetProperty(propName).PropertyType == typeof(Xv2CoreLib.BSA.EepkType))
                Instance.GetType().GetProperty(propName).SetValue(Instance, (Xv2CoreLib.BSA.EepkType)newValue, null);
        }

        public void ReplaceValue(int newValue)
        {
            if (Instance.GetType().GetProperty(propName).PropertyType == typeof(int))
                Instance.GetType().GetProperty(propName).SetValue(Instance, newValue, null);
            else if (Instance.GetType().GetProperty(propName).PropertyType == typeof(uint))
                Instance.GetType().GetProperty(propName).SetValue(Instance, (uint)newValue, null);
            else if (Instance.GetType().GetProperty(propName).PropertyType == typeof(ushort))
                Instance.GetType().GetProperty(propName).SetValue(Instance, (ushort)newValue, null);
            else if (Instance.GetType().GetProperty(propName).PropertyType == typeof(short))
                Instance.GetType().GetProperty(propName).SetValue(Instance, (short)newValue, null);
            else if (Instance.GetType().GetProperty(propName).PropertyType.IsEnum)
                Instance.GetType().GetProperty(propName).SetValue(Instance, newValue, null);
            else
                throw new InvalidOperationException($"IdReference.ReplaceValue: Invalid PropertyType = {Instance.GetType().GetProperty(propName).PropertyType}");
        }
    }
    
}
