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
            if (bsaEntry.SubEntries?.CollisionEntries != null)
            {
                foreach (var unk1 in bsaEntry.SubEntries.CollisionEntries)
                {
                    if (CopyEffect((BAC_Type8.EepkTypeEnum)unk1.EepkType, unk1.EffectID, unk1.SkillID, move))
                    {
                        ValueRefs.Add(new ValueReference(unk1, nameof(unk1.EffectID), ValueReference.InstanceRefType.Eepk));
                        ValueRefs.Add(new ValueReference(unk1, nameof(unk1.EepkType), ValueReference.InstanceRefType.Eepk, ValueReference.Mode.Type));
                        ValueRefs.Add(new ValueReference(unk1, nameof(unk1.SkillID), ValueReference.InstanceRefType.Eepk, ValueReference.Mode.SkillId));
                    }
                }
            }

            //Types
            if (bsaEntry.IBsaTypes == null) return;

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
                var entryCopy = entry.Copy();
                Secondary.BsaEntries.Add(entryCopy);
                CopyBsaEntryReferences(entryCopy, move);
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

        private List<IUndoRedo> PasteBsaEntries(IList<BSA_Entry> bsaEntries, Move move)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var bsaEntry in bsaEntries)
            {
                var pastedEntry = bsaEntry.Copy();
                int oldId = pastedEntry.SortID;
                int newId = move.Files.BsaFile.File.AddEntry(pastedEntry);
                ReplaceIdReference(ValueReference.InstanceRefType.Bsa, oldId, newId);
                undos.Add(new UndoableListAdd<BSA_Entry>(move.Files.BsaFile.File.BSA_Entries, pastedEntry));
            }

            return undos;
        }

    }
}
