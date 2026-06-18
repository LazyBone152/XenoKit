using System;
using System.Collections.Generic;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Helper;
using Xv2CoreLib;
using Xv2CoreLib.ACB;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BCM;
using Xv2CoreLib.BDM;
using Xv2CoreLib.BSA;
using Xv2CoreLib.EAN;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Editor
{
    public partial class CopyItem
    {
        private bool CopyBacEntry(int bacId, Move move)
        {
            if(bacId != -1 && bacId != ushort.MaxValue)
            {
                var bacEntry = Files.Instance.SelectedItem.SelectedBacFile.File.GetEntry(bacId);

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
                    var projectileCopy = projectile.Copy();
                    Secondary.BsaEntries.Add(projectileCopy);
                    CopyBsaEntryReferences(projectileCopy, move);
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
                    var projectileCopy = projectile.Copy();
                    Secondary.BsaEntries.Add(projectileCopy);
                    CopyBsaEntryReferences(projectileCopy, move);
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
                    Secondary.SeAcbFile.CopyCue(cueId, acbFile.AcbFile, true);

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
                else if (move.MoveType == Move.Type.CMN)
                    valueRef.ReplaceValue(0);

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
                        valueRef.SetEnum((int)BAC_Type0.EanTypeEnum.Skill);
                    else if (refType == ValueReference.InstanceRefType.Cam)
                        valueRef.SetEnum((int)BAC_Type10.EanTypeEnum.Skill);
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
                        valueRef.SetEnum((int)BAC_Type0.EanTypeEnum.Character);
                    else if (refType == ValueReference.InstanceRefType.Cam)
                        valueRef.SetEnum((int)BAC_Type10.EanTypeEnum.Character);
                }

            }

            //Remove Type references
            ValueRefs.RemoveAll(x => refs.Contains(x));
        }

    }
}
