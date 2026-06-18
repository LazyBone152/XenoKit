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
        public List<IUndoRedo> PasteIntoMove_Sub(BAC_Entry mainEntry, Move move, bool copyReferences)
        {
            if (entryType != EntryType.Sub)
                throw new InvalidOperationException($"{nameof(CopyItem)}.{nameof(PasteIntoMove_Sub)}: function can only be called with entryType = Sub!");

            RemoveDuplicates();
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if(move.MoveGuid != MoveGuid)
                undos.AddRange(PasteReferences(move, copyReferences));

            foreach(var bacEntry in Primary.BacEntries[0].IBacTypes)
            {
                var bacType = (IBacType)bacEntry.Copy();
                if(ResetTimeLineLayers)
                    bacType.Layer = -1;

                undos.Add(mainEntry.AddEntry(bacType));
                //mainEntry.IBacTypes.Add(bacEntry);
                //undos.Add(new UndoableListAdd<IBacType>(mainEntry.IBacTypes, bacEntry));
            }

            return undos;
        }

        public List<IUndoRedo> PasteIntoMove_Main(Move move, bool copyReferences, BAC_Entry bacEntryToReplace = null)
        {
            if (entryType != EntryType.Main)
                throw new InvalidOperationException($"{nameof(CopyItem)}.{nameof(PasteIntoMove_Main)}: function can only be called with entryType = Main!");

            //move the parameter must match the SelectedItem in the Outliner. This is a quick dirty hack to account for selectable BAC files, which did not exist when this copy system was originally written
            //TODO: Rework this
            if (Files.Instance.SelectedMove != move)
                throw new InvalidOperationException("PasteIntoMove_Main: The move passed into the method must be the one selected in the outliner.");

            RemoveDuplicates();
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if(move.MoveGuid != MoveGuid)
                undos.AddRange(PasteReferences(move, copyReferences));

            undos.AddRange(PasteEntries(move, Primary, bacEntryToReplace));

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

        private List<IUndoRedo> PasteEntries(Move move, CopyEntries entries, BAC_Entry bacEntryToReplace = null)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            //BAC
            undos.AddRange(PasteBacEntries(entries.BacEntries, move, bacEntryToReplace));

            //BDM
            undos.AddRange(PasteBdmEntries(entries.BdmEntries, move));

            if(move.MoveType == Move.Type.Skill || move.MoveType == Move.Type.CMN)
            {
                //SHOT.BDM
                undos.AddRange(PasteShotBdmEntries(entries.ShotBdmEntries, move));

                //BSA
                undos.AddRange(PasteBsaEntries(entries.BsaEntries, move));
            }

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
                //If CUE already exists in any of the SE files, then skip add and use that ID
                if (move.Files.SeAcbFile.Any(x => x.File.AcbFile.GetCueId(cue.InstanceGuid) != uint.MaxValue))
                {
                    var seFile = move.Files.SeAcbFile.FirstOrDefault(x => x.File.AcbFile.GetCueId(cue.InstanceGuid) != uint.MaxValue);
                    ReplaceIdReference(ValueReference.InstanceRefType.SeAcb, (int)cue.ID, (int)seFile.File.AcbFile.GetCueId(cue.InstanceGuid));
                    continue;
                }

                //Copy cue
                int oldId = (int)cue.ID;
                int newId = GeneralHelpers.AssignCommonCueId(move.Files.SeAcbFile);
                cue.ID = (uint)newId;

                foreach(var acb in move.Files.SeAcbFile)
                {
                    undos.AddRange(acb.File.AcbFile.CopyCue(newId, entries.SeAcbFile, true));
                }

                ReplaceIdReference(ValueReference.InstanceRefType.SeAcb, oldId, newId);

            }

            foreach (var acb in move.Files.SeAcbFile)
            {
                acb.File.Refresh();
                undos.Add(new UndoActionDelegate(acb.File, nameof(acb.File.Refresh), true));
            }

            //EAN
            foreach (var animation in entries.Animations)
            {
                var ean = move.Files.GetDefaultOrFirstEanFile();

                if(ean != null)
                {
                    EAN_Animation newAnimation = animation.DeserializeToEan(ean.Skeleton);

                    int oldId = animation.ID;
                    int newId = ean.AddEntry(newAnimation);
                    ReplaceIdReference(ValueReference.InstanceRefType.Ean, oldId, newId);
                    undos.Add(new UndoableListAdd<EAN_Animation>(ean.Animations, newAnimation));
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

    }
}
