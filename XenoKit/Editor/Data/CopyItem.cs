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
    public partial class CopyItem
    {
        public Guid MoveGuid;

        public FileType fileType;
        public EntryType entryType;
        public Move.Type MoveType;
        public int SkillID;

        public readonly CopyEntries Primary = new CopyEntries(); //Includes all entries directly copied
        public readonly CopyEntries Secondary = new CopyEntries(); //Includes all entries referenced in Primary (Optional)

        public bool ResetTimeLineLayers = true;

        //ID References, used for changing IDs before pasting
        private readonly List<ValueReference> ValueRefs = new List<ValueReference>();

        public CopyItem(IList<BAC_Entry> bacEntries, Move move)
        {
            fileType = FileType.Bac;
            entryType = EntryType.Main;
            MoveType = move.MoveType;
            SkillID = move.SkillID;
            MoveGuid = move.MoveGuid;

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
            MoveGuid = move.MoveGuid;

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

        public CopyItem(IList<BSA_Entry> bsaEntries, Move move)
        {
            fileType = FileType.Bsa;
            entryType = EntryType.Main;
            MoveType = move.MoveType;
            SkillID = move.SkillID;
            MoveGuid = move.MoveGuid;

            foreach (BSA_Entry bsaEntry in bsaEntries)
                CopyBsaEntryReferences(bsaEntry, move);

            Primary.BsaEntries.AddRange(bsaEntries);
            RemoveDuplicates();
        }

        public CopyItem(IList<IBsaType> bsaTypes, Move move)
        {
            fileType = FileType.Bsa;
            entryType = EntryType.Sub;
            MoveType = move.MoveType;
            SkillID = move.SkillID;
            MoveGuid = move.MoveGuid;

            BSA_Entry temp = new BSA_Entry();
            temp.IBsaTypes = new AsyncObservableCollection<IBsaType>(bsaTypes);

            CopyBsaEntryReferences(temp, move);
            Primary.BsaEntries.Add(temp);
            RemoveDuplicates();
        }

        public CopyItem(IList<BDM_Entry> bdmEntries, Move move, bool shotBdm = false)
        {
            fileType = shotBdm ? FileType.ShotBdm : FileType.Bdm;
            entryType = EntryType.Main;
            MoveType = move.MoveType;
            SkillID = move.SkillID;
            MoveGuid = move.MoveGuid;

            foreach (BDM_Entry bdmEntry in bdmEntries)
                CopyBdmEntryReferences(bdmEntry, move, shotBdm);

            if (shotBdm)
                Primary.ShotBdmEntries.AddRange(bdmEntries);
            else
                Primary.BdmEntries.AddRange(bdmEntries);

            RemoveDuplicates();
        }

        public CopyItem(IList<Type0SubEntry> bdmSubEntries, Move move, bool shotBdm = false)
        {
            fileType = shotBdm ? FileType.ShotBdm : FileType.Bdm;
            entryType = EntryType.Sub;
            MoveType = move.MoveType;
            SkillID = move.SkillID;
            MoveGuid = move.MoveGuid;

            BDM_Entry temp = new BDM_Entry();
            temp.Type0Entries = bdmSubEntries.ToList();
            CopyBdmEntryReferences(temp, move, shotBdm);

            if (shotBdm)
                Primary.ShotBdmEntries.Add(temp);
            else
                Primary.BdmEntries.Add(temp);

            RemoveDuplicates();
        }

        public CopyItem(IList<BCM_Entry> bcmEntries, Move move)
        {
            fileType = FileType.Bcm;
            entryType = EntryType.Main;
            MoveType = move.MoveType;
            SkillID = move.SkillID;
            MoveGuid = move.MoveGuid;
            Primary.BcmEntries.AddRange(bcmEntries);
        }


        #region Copy
        //BAC








        //BDM

        //BSA




        #endregion

        #region CopyGeneral







        private bool CopyEffect(BAC_Type8.EepkTypeEnum eepkType, uint effectId, ushort skillId, Move move)
        {
            if (effectId > ushort.MaxValue)
                return false;

            return CopyEffect(eepkType, (int)effectId, skillId, move);
        }
        #endregion

        #region Paste








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



        #endregion

        #region Helpers
        public int NumMainEntries()
        {
            int total = 0;
            total += Primary.BacEntries.Count;
            total += Primary.BdmEntries.Count;
            total += Primary.ShotBdmEntries.Count;
            total += Primary.BsaEntries.Count;
            total += Primary.BcmEntries.Count;
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
                    return $"{Primary.BacEntries.Count} BAC Entries";
                else if (fileType == FileType.Bsa)
                    return $"{Primary.BsaEntries.Count} BSA Entries";
                else if (fileType == FileType.Bdm)
                    return $"{Primary.BdmEntries.Count} BDM Entries";
                else if (fileType == FileType.ShotBdm)
                    return $"{Primary.ShotBdmEntries.Count} Shot.BDM Entries";
                else if (fileType == FileType.Bcm)
                    return $"{Primary.BcmEntries.Count} BCM Entries";
            }
            else if(entryType == EntryType.Sub)
            {
                try
                {
                    if (fileType == FileType.Bac)
                        return $"{Primary.BacEntries[0].IBacTypes.Count} BAC Types";
                    else if (fileType == FileType.Bsa)
                        return $"{Primary.BsaEntries[0].IBsaTypes.Count} BSA Types";
                    else if (fileType == FileType.Bdm)
                        return $"{Primary.BdmEntries[0].Type0Entries.Count} BDM SubEntries";
                    else if (fileType == FileType.ShotBdm)
                        return $"{Primary.ShotBdmEntries[0].Type0Entries.Count} Shot.BDM SubEntries";
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
            return $"BAC Entries: {Secondary.BacEntries.Count}\n" +
                $"BDM Entries: {Secondary.BdmEntries.Count}\n" +
                $"BSA Entries: {Secondary.BsaEntries.Count}\n" +
                $"Shot.BDM Entries: {Secondary.ShotBdmEntries.Count}\n" +
                $"Animations: {Secondary.Animations.Count}\n" +
                $"Cameras: {Secondary.Cameras.Count}\n" +
                $"SE Cues: {Secondary.SeAcbFile.Cues.Count}\n" +
                $"Effects: {Secondary.Effects.Count}\n";
        }
        #endregion
    }

}
