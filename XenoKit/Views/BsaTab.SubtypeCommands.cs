using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using XenoKit.Editor;
using XenoKit.Editor.Undo;
using XenoKit.Engine;
using XenoKit.Engine.Scripting.BSA;
using XenoKit.ViewModel.BSA;
using XenoKit.Windows;
using Xv2CoreLib;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Views
{
    public partial class BsaTab : UserControl, INotifyPropertyChanged
    {
        public RelayCommand<string> AddSubtypeCommand => new RelayCommand<string>(AddSubtype, _ => SelectedEntry != null);

        public RelayCommand DuplicateSubtypeCommand => new RelayCommand(DuplicateSubtype, () => SelectedSubtypeRow != null);

        public RelayCommand CopySubtypeCommand => new RelayCommand(CopySubtype, () => SelectedSubtypeRow != null);

        public RelayCommand PasteSubtypeCommand => new RelayCommand(PasteSubtype, CanPasteSubtype);

        public RelayCommand DeleteSubtypeCommand => new RelayCommand(DeleteSubtype, () => SelectedSubtypeRow != null);

        public RelayCommand MoveSubtypeUpCommand => new RelayCommand(() => MoveSubtype(-1), () => CanMoveSubtype(-1));

        public RelayCommand MoveSubtypeDownCommand => new RelayCommand(() => MoveSubtype(1), () => CanMoveSubtype(1));

        private void AddSubtype(string typeText)
        {
            if (SelectedEntry == null) return;
            EnsureSubEntries();

            if (typeText == "Collision")
            {
                BSA_Collision collision = new BSA_Collision();
                SelectedEntry.SubEntries.CollisionEntries.Add(collision);
                UndoManager.Instance.AddUndo(new UndoableListAdd<BSA_Collision>(SelectedEntry.SubEntries.CollisionEntries, collision, "BSA Collision Add"));
                SelectSubtypeSource(collision);
                return;
            }

            if (typeText == "Expiration")
            {
                BSA_Expiration expiration = new BSA_Expiration();
                SelectedEntry.SubEntries.ExpirationEntries.Add(expiration);
                UndoManager.Instance.AddUndo(new UndoableListAdd<BSA_Expiration>(SelectedEntry.SubEntries.ExpirationEntries, expiration, "BSA Expiration Add"));
                SelectSubtypeSource(expiration);
                return;
            }

            if (!int.TryParse(typeText, out int typeId)) return;

            IBsaType type = CreateBsaType(typeId);
            if (type == null) return;

            SelectedEntry.IBsaTypes.Add(type);
            UndoManager.Instance.AddUndo(new UndoableListAdd<IBsaType>(SelectedEntry.IBsaTypes, type, "BSA Type Add"));
            SelectSubtypeSource(type);
        }

        private void DuplicateSubtype()
        {
            if (SelectedSubtypeRow == null) return;
            EnsureSubEntries();

            if (SelectedSubtypeRow.Source is IBsaType type)
            {
                IBsaType clone = CloneBsaType(type);
                SelectedEntry.IBsaTypes.Add(clone);
                UndoManager.Instance.AddUndo(new UndoableListAdd<IBsaType>(SelectedEntry.IBsaTypes, clone, "BSA Type Duplicate"));
                SelectSubtypeSource(clone);
            }
            else if (SelectedSubtypeRow.Source is BSA_Collision collision)
            {
                BSA_Collision clone = collision.Copy();
                SelectedEntry.SubEntries.CollisionEntries.Add(clone);
                UndoManager.Instance.AddUndo(new UndoableListAdd<BSA_Collision>(SelectedEntry.SubEntries.CollisionEntries, clone, "BSA Collision Duplicate"));
                SelectSubtypeSource(clone);
            }
            else if (SelectedSubtypeRow.Source is BSA_Expiration expiration)
            {
                BSA_Expiration clone = expiration.Copy();
                SelectedEntry.SubEntries.ExpirationEntries.Add(clone);
                UndoManager.Instance.AddUndo(new UndoableListAdd<BSA_Expiration>(SelectedEntry.SubEntries.ExpirationEntries, clone, "BSA Expiration Duplicate"));
                SelectSubtypeSource(clone);
            }
        }

        private void CopySubtype()
        {
            if (SelectedSubtypeRow?.Source is IBsaType type)
                Clipboard.SetData(ClipboardConstants.BsaType_CopyItem, CloneBsaType(type));
            else if (SelectedSubtypeRow?.Source is BSA_Collision collision)
                Clipboard.SetData(BsaCollisionCopyItem, collision.Copy());
            else if (SelectedSubtypeRow?.Source is BSA_Expiration expiration)
                Clipboard.SetData(BsaExpirationCopyItem, expiration.Copy());
        }

        private void PasteSubtype()
        {
            if (SelectedEntry == null) return;
            EnsureSubEntries();

            if (Clipboard.ContainsData(ClipboardConstants.BsaType_CopyItem))
            {
                IBsaType type = CloneBsaType((IBsaType)Clipboard.GetData(ClipboardConstants.BsaType_CopyItem));
                SelectedEntry.IBsaTypes.Add(type);
                UndoManager.Instance.AddUndo(new UndoableListAdd<IBsaType>(SelectedEntry.IBsaTypes, type, "BSA Type Paste"));
                SelectSubtypeSource(type);
            }
            else if (Clipboard.ContainsData(BsaCollisionCopyItem))
            {
                BSA_Collision collision = ((BSA_Collision)Clipboard.GetData(BsaCollisionCopyItem)).Copy();
                SelectedEntry.SubEntries.CollisionEntries.Add(collision);
                UndoManager.Instance.AddUndo(new UndoableListAdd<BSA_Collision>(SelectedEntry.SubEntries.CollisionEntries, collision, "BSA Collision Paste"));
                SelectSubtypeSource(collision);
            }
            else if (Clipboard.ContainsData(BsaExpirationCopyItem))
            {
                BSA_Expiration expiration = ((BSA_Expiration)Clipboard.GetData(BsaExpirationCopyItem)).Copy();
                SelectedEntry.SubEntries.ExpirationEntries.Add(expiration);
                UndoManager.Instance.AddUndo(new UndoableListAdd<BSA_Expiration>(SelectedEntry.SubEntries.ExpirationEntries, expiration, "BSA Expiration Paste"));
                SelectSubtypeSource(expiration);
            }
        }

        private void DeleteSubtype()
        {
            if (SelectedSubtypeRow == null || SelectedEntry == null) return;
            EnsureSubEntries();

            object removedSource = SelectedSubtypeRow.Source;
            if (removedSource is IBsaType type)
            {
                UndoManager.Instance.AddUndo(new UndoableListRemove<IBsaType>(SelectedEntry.IBsaTypes, type, "BSA Type Delete"));
                SelectedEntry.IBsaTypes.Remove(type);
            }
            else if (removedSource is BSA_Collision collision)
            {
                UndoManager.Instance.AddUndo(new UndoableListRemove<BSA_Collision>(SelectedEntry.SubEntries.CollisionEntries, collision, "BSA Collision Delete"));
                SelectedEntry.SubEntries.CollisionEntries.Remove(collision);
            }
            else if (removedSource is BSA_Expiration expiration)
            {
                UndoManager.Instance.AddUndo(new UndoableListRemove<BSA_Expiration>(SelectedEntry.SubEntries.ExpirationEntries, expiration, "BSA Expiration Delete"));
                SelectedEntry.SubEntries.ExpirationEntries.Remove(expiration);
            }

            SelectedSubtypeRow = null;
            RebuildSubtypeRows();
        }

        private bool CanMoveSubtype(int direction)
        {
            if (SelectedSubtypeRow == null || SelectedEntry == null) return false;

            if (SelectedSubtypeRow.Source is IBsaType type && SelectedEntry.IBsaTypes != null)
            {
                int index = SelectedEntry.IBsaTypes.IndexOf(type);
                int newIndex = index + direction;
                return index >= 0 && newIndex >= 0 && newIndex < SelectedEntry.IBsaTypes.Count;
            }

            EnsureSubEntries();

            if (SelectedSubtypeRow.Source is BSA_Collision collision)
            {
                int index = SelectedEntry.SubEntries.CollisionEntries.IndexOf(collision);
                int newIndex = index + direction;
                return index >= 0 && newIndex >= 0 && newIndex < SelectedEntry.SubEntries.CollisionEntries.Count;
            }

            if (SelectedSubtypeRow.Source is BSA_Expiration expiration)
            {
                int index = SelectedEntry.SubEntries.ExpirationEntries.IndexOf(expiration);
                int newIndex = index + direction;
                return index >= 0 && newIndex >= 0 && newIndex < SelectedEntry.SubEntries.ExpirationEntries.Count;
            }

            return false;
        }

        private void MoveSubtype(int direction)
        {
            if (SelectedSubtypeRow == null || SelectedEntry == null) return;
            EnsureSubEntries();

            if (SelectedSubtypeRow.Source is IBsaType type)
            {
                int oldIndex = SelectedEntry.IBsaTypes.IndexOf(type);
                int newIndex = oldIndex + direction;
                UndoManager.Instance.AddUndo(new UndoableListMove<IBsaType>(SelectedEntry.IBsaTypes, oldIndex, newIndex, "BSA Type Move"));
                SelectedEntry.IBsaTypes.Move(oldIndex, newIndex);
                SelectSubtypeSource(type);
            }
            else if (SelectedSubtypeRow.Source is BSA_Collision collision)
            {
                int oldIndex = SelectedEntry.SubEntries.CollisionEntries.IndexOf(collision);
                int newIndex = oldIndex + direction;
                UndoManager.Instance.AddUndo(new ListMoveUndo<BSA_Collision>(SelectedEntry.SubEntries.CollisionEntries, oldIndex, newIndex, "BSA Collision Move"));
                SelectSubtypeSource(collision);
            }
            else if (SelectedSubtypeRow.Source is BSA_Expiration expiration)
            {
                int oldIndex = SelectedEntry.SubEntries.ExpirationEntries.IndexOf(expiration);
                int newIndex = oldIndex + direction;
                UndoManager.Instance.AddUndo(new ListMoveUndo<BSA_Expiration>(SelectedEntry.SubEntries.ExpirationEntries, oldIndex, newIndex, "BSA Expiration Move"));
                SelectSubtypeSource(expiration);
            }
        }

    }
}
