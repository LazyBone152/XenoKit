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
        private void CopyBacEntryReferences(BAC_Entry bacEntry, Move move)
        {
            foreach (var bacType in bacEntry.IBacTypes)
            {
                switch (bacType)
                {
                    case BAC_Type0 type0:
                        CopyBacType0References(type0, move);
                        break;
                    case BAC_Type1 type1:
                        CopyBacType1References(type1, move);
                        break;
                    case BAC_Type8 type8:
                        CopyBacType8References(type8, move);
                        break;
                    case BAC_Type9 type9:
                        CopyBacType9References(type9, move);
                        break;
                    case BAC_Type10 type10:
                        CopyBacType10References(type10, move);
                        break;
                    case BAC_Type11 type11:
                        CopyBacType11References(type11, move);
                        break;
                    case BAC_Type17 type17:
                        CopyBacType17References(type17, move);
                        break;
                }
            }
        }

        private void CopyBacType0References(BAC_Type0 bacType, Move move)
        {
            if(bacType.EanIndex != ushort.MaxValue)
            {
                var ean = Files.Instance.GetEanFile(bacType.EanType, move, null, false, false);
                var animation = move.TryGetAnimation(bacType.EanType, bacType.EanIndex);

                if (animation != null && !Secondary.Animations.Any(x => x.ID == animation?.ID_UShort))
                    Secondary.Animations.Add(new SerializedAnimation(animation, ean.Skeleton));

                if(animation != null)
                {
                    ValueRefs.Add(new ValueReference(bacType, nameof(bacType.EanIndex), ValueReference.InstanceRefType.Ean));
                    ValueRefs.Add(new ValueReference(bacType, nameof(bacType.EanType), ValueReference.InstanceRefType.Ean, ValueReference.Mode.Type));
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
                var camera = move.TryGetCamera(bacType.EanType, bacType.EanIndex);

                if (camera != null && !Secondary.Cameras.Any(x => x.ID_UShort == camera?.ID_UShort))
                    Secondary.Cameras.Add(camera);

                if(camera != null)
                {
                    ValueRefs.Add(new ValueReference(bacType, nameof(bacType.EanIndex), ValueReference.InstanceRefType.Cam));
                    ValueRefs.Add(new ValueReference(bacType, nameof(bacType.EanType), ValueReference.InstanceRefType.Cam, ValueReference.Mode.Type));
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

        private List<IUndoRedo> PasteBacEntries(IList<BAC_Entry> bacEntries, Move move, BAC_Entry bacEntryToReplace = null)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if(bacEntryToReplace != null)
            {
                var bacEntry = bacEntries[0].Copy();
                undos.Add(new UndoableProperty<BAC_Entry>(nameof(BAC_Entry.IBacTypes), bacEntryToReplace, bacEntryToReplace.IBacTypes, bacEntry.IBacTypes));
                bacEntryToReplace.IBacTypes = bacEntry.IBacTypes;
            }
            else
            {
                foreach (var bacEntry in bacEntries)
                {
                    var bacEntryCopy = bacEntry.Copy();
                    int oldId = bacEntryCopy.SortID;
                    int newId = Files.Instance.SelectedItem.SelectedBacFile.File.AddEntry(bacEntryCopy);
                    ReplaceIdReference(ValueReference.InstanceRefType.Bac, oldId, newId);
                    undos.Add(new UndoableListAdd<BAC_Entry>(Files.Instance.SelectedItem.SelectedBacFile.File.BacEntries, bacEntryCopy));
                }
            }

            return undos;
        }

    }
}
