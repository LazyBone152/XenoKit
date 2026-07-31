using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using XenoKit.Helper.Find;
using Xv2CoreLib.BAC;
using LB_Common;
using static Xv2CoreLib.Xenoverse2;
using XenoKit.Editor;
using Xv2CoreLib.Resource.UndoRedo;
using GalaSoft.MvvmLight.CommandWpf;
using Xv2CoreLib.BCM;

namespace XenoKit.Windows
{
    public partial class FindAndReplace
    {
        private BCM_Entry FindBcmValue(BCM_File file, string valueName, object value, BCM_Entry previousEntry, bool isNot)
        {
            List<BCM_Entry> entries = FlattenBcmEntries(file.BCMEntries).ToList();
            if (entries.Count == 0) return null;

            int startIndex = 0;
            BCM_Entry selectedEntry = mainWindow.bcmTabView.SelectedEntry;
            int previousIndex = previousEntry != null ? entries.IndexOf(previousEntry) : entries.IndexOf(selectedEntry);
            if (previousIndex >= 0)
                startIndex = previousIndex + 1;

            for (int offset = 0; offset < entries.Count; offset++)
            {
                BCM_Entry entry = entries[(startIndex + offset) % entries.Count];
                object entryValue = entry.GetType().GetProperty(valueName)?.GetValue(entry);
                bool matches = ValuesEqual(entryValue, value, SelectedValue.valueType);
                if (isNot) matches = !matches;
                if (matches) return entry;
            }

            return null;
        }

        private List<IUndoRedo> ReplaceBcmValue(BCM_File file, string valueName, object valueToFind, object valueToReplace, out int numReplaced)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            numReplaced = 0;

            foreach (BCM_Entry entry in FlattenBcmEntries(file.BCMEntries))
            {
                if (IsRootBcmEntry(file, entry)) continue;

                var property = entry.GetType().GetProperty(valueName);
                if (property == null) continue;

                object oldValue = property.GetValue(entry);
                if (!ValuesEqual(oldValue, valueToFind, SelectedValue.valueType)) continue;
                if (ValuesEqual(oldValue, valueToReplace, SelectedValue.valueType)) continue;

                undos.Add(new UndoablePropertyGeneric(valueName, entry, oldValue, valueToReplace, SelectedValue.ValueName));
                property.SetValue(entry, valueToReplace);
                numReplaced++;
            }

            return undos;
        }

        private IEnumerable<BCM_Entry> FlattenBcmEntries(IEnumerable<BCM_Entry> entries)
        {
            if (entries == null) yield break;

            foreach (BCM_Entry entry in entries)
            {
                yield return entry;

                foreach (BCM_Entry child in FlattenBcmEntries(entry.BCMEntries))
                    yield return child;
            }
        }

        private bool IsRootBcmEntry(BCM_File file, BCM_Entry entry)
        {
            return file?.BCMEntries != null && entry != null && file.BCMEntries.Contains(entry);
        }

        private static bool ValuesEqual(object first, object second, Type type)
        {
            if (first == null || second == null) return Equals(first, second);

            if (type == typeof(float))
                return Math.Abs((float)first - (float)second) < 0.000001f;

            if (type == typeof(double))
                return Math.Abs((double)first - (double)second) < 0.000001d;

            return Equals(first, second);
        }

        private static List<Value> CreateBcmValues()
        {
            return new List<Value>
            {
                BcmValue("State Id", nameof(BCM_Entry.Index)),
                BcmValue("Directional Input", nameof(BCM_Entry.DirectionalInput)),
                BcmValue("Button Input", nameof(BCM_Entry.ButtonInput)),
                BcmValue("Hold Down Conditions", nameof(BCM_Entry.HoldDownConditions)),
                BcmValue("Opponent Size", nameof(BCM_Entry.OpponentSizeConditions)),
                BcmValue("Minimum Loop Duration", nameof(BCM_Entry.MinimumLoopDuration)),
                BcmValue("Maximum Loop Duration", nameof(BCM_Entry.MaximumLoopDuration)),
                BcmValue("Primary Conditions", nameof(BCM_Entry.PrimaryActivatorConditions)),
                BcmValue("Activator State", nameof(BCM_Entry.ActivatorState)),
                BcmValue("Primary BAC", nameof(BCM_Entry.BacEntryPrimary)),
                BcmValue("Airborne BAC", nameof(BCM_Entry.BacEntryAirborne)),
                BcmValue("Charge BAC", nameof(BCM_Entry.BacEntryCharge)),
                BcmValue("User Connect BAC", nameof(BCM_Entry.BacEntryUserConnect)),
                BcmValue("Victim Connect BAC", nameof(BCM_Entry.BacEntryVictimConnect)),
                BcmValue("Targeting Override BAC", nameof(BCM_Entry.BacEntryTargetingOverride)),
                BcmValue("Mode", nameof(BCM_Entry.RandomFlag)),
                BcmValue("Ki Cost", nameof(BCM_Entry.I_64)),
                BcmValue("Receiver Link Id", nameof(BCM_Entry.ReceiverLinkID)),
                BcmValue("Stamina Cost", nameof(BCM_Entry.StaminaCost)),
                BcmValue("Ki Required", nameof(BCM_Entry.KiRequired)),
                BcmValue("Health Required", nameof(BCM_Entry.HealthRequired)),
                BcmValue("Transformation Stage", nameof(BCM_Entry.TransStage)),
                BcmValue("CUS Aura", nameof(BCM_Entry.CusAura)),
                BcmValue("I_00", nameof(BCM_Entry.I_00)),
                BcmValue("I_36", nameof(BCM_Entry.I_36)),
                BcmValue("I_68", nameof(BCM_Entry.I_68)),
                BcmValue("I_72", nameof(BCM_Entry.I_72)),
                BcmValue("I_80", nameof(BCM_Entry.I_80)),
                BcmValue("I_88", nameof(BCM_Entry.I_88)),
                BcmValue("I_104", nameof(BCM_Entry.I_104)),
                BcmValue("I_108", nameof(BCM_Entry.I_108))
            };
        }

        private static Value BcmValue(string displayName, string propertyName)
        {
            return new Value(typeof(BCM_Entry).GetProperty(propertyName).PropertyType, propertyName, null, null, displayName);
        }

    }
}
