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
        private void CopyBdmEntryReferences(BDM_Entry bdmEntry, Move move, bool shotBdm = false)
        {
            if (bdmEntry.Type0Entries == null) return;

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

    }
}
