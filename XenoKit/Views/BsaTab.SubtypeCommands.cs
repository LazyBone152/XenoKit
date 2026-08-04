using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using XenoKit.Editor;
using XenoKit.Windows;
using Xv2CoreLib;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Views
{
    public partial class BsaTab : UserControl, INotifyPropertyChanged
    {
        // Subtype reorder commands were removed. The BSA format writes one header per type, so cross-type
        // order cannot round-trip. AddIBsaType keeps the list in TypeID order, matching what gets saved.

        public RelayCommand<string> AddSubtypeCommand => new RelayCommand<string>(AddSubtype, _ => SelectedEntry != null);
        public RelayCommand DuplicateSubtypeCommand => new RelayCommand(DuplicateSubtype, () => SelectedSubtypeRow != null);
        public RelayCommand CopySubtypeCommand => new RelayCommand(CopySubtype, () => SelectedSubtypeRow != null);
        public RelayCommand PasteSubtypeCommand => new RelayCommand(PasteSubtype, CanPasteSubtype);
        public RelayCommand DeleteSubtypeCommand => new RelayCommand(DeleteSubtype, () => SelectedSubtypeRow != null);

        private void AddSubtype(string typeText)
        {
            if (SelectedEntry == null) return;
            InitSubEntries();

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

            UndoManager.Instance.AddUndo(SelectedEntry.AddIBsaType(type));
            SelectSubtypeSource(type);
        }

        private void DuplicateSubtype()
        {
            if (SelectedEntry == null || SelectedSubtypeSource == null) return;
            InitSubEntries();

            switch (SelectedSubtypeSource)
            {
                case IBsaType type:
                    {
                        IBsaType clone = type.Copy();
                        UndoManager.Instance.AddUndo(SelectedEntry.AddIBsaType(clone));
                        SelectSubtypeSource(clone);
                        break;
                    }
                case BSA_Collision collision:
                    {
                        BSA_Collision clone = collision.Copy();
                        SelectedEntry.SubEntries.CollisionEntries.Add(clone);
                        UndoManager.Instance.AddUndo(new UndoableListAdd<BSA_Collision>(SelectedEntry.SubEntries.CollisionEntries, clone, "BSA Collision Duplicate"));
                        SelectSubtypeSource(clone);
                        break;
                    }
                case BSA_Expiration expiration:
                    {
                        BSA_Expiration clone = expiration.Copy();
                        SelectedEntry.SubEntries.ExpirationEntries.Add(clone);
                        UndoManager.Instance.AddUndo(new UndoableListAdd<BSA_Expiration>(SelectedEntry.SubEntries.ExpirationEntries, clone, "BSA Expiration Duplicate"));
                        SelectSubtypeSource(clone);
                        break;
                    }
            }
        }

        private void CopySubtype()
        {
            switch (SelectedSubtypeSource)
            {
                case IBsaType type when files.SelectedMove != null:
                    XenoKitClipboard.SetData(ClipboardConstants.BsaType_CopyItem, new CopyItem(new List<IBsaType> { type }, files.SelectedMove));
                    break;
                case BSA_Collision collision:
                    XenoKitClipboard.SetData(ClipboardConstants.BsaCollision_CopyItem, collision.Copy());
                    break;
                case BSA_Expiration expiration:
                    XenoKitClipboard.SetData(ClipboardConstants.BsaExpiration_CopyItem, expiration.Copy());
                    break;
            }
        }

        private void PasteSubtype()
        {
            if (SelectedEntry == null) return;
            InitSubEntries();

            if (XenoKitClipboard.TryGetData(ClipboardConstants.BsaCollision_CopyItem, out BSA_Collision collision))
            {
                SelectedEntry.SubEntries.CollisionEntries.Add(collision);
                UndoManager.Instance.AddUndo(new UndoableListAdd<BSA_Collision>(SelectedEntry.SubEntries.CollisionEntries, collision, "BSA Collision Paste"));
                SelectSubtypeSource(collision);
                return;
            }

            if (XenoKitClipboard.TryGetData(ClipboardConstants.BsaExpiration_CopyItem, out BSA_Expiration expiration))
            {
                SelectedEntry.SubEntries.ExpirationEntries.Add(expiration);
                UndoManager.Instance.AddUndo(new UndoableListAdd<BSA_Expiration>(SelectedEntry.SubEntries.ExpirationEntries, expiration, "BSA Expiration Paste"));
                SelectSubtypeSource(expiration);
                return;
            }

            if (files.SelectedMove != null && XenoKitClipboard.TryGetData(ClipboardConstants.BsaType_CopyItem, out CopyItem copyItem))
            {
                new PasteCopyItem(copyItem, files.SelectedMove, SelectedEntry).ShowDialog();
                RebuildSubtypeRows();
            }
        }

        private void DeleteSubtype()
        {
            if (SelectedEntry == null || SelectedSubtypeSource == null) return;
            InitSubEntries();

            switch (SelectedSubtypeSource)
            {
                case IBsaType type:
                    UndoManager.Instance.AddUndo(new UndoableListRemove<IBsaType>(SelectedEntry.IBsaTypes, type, "BSA Subtype Delete"));
                    SelectedEntry.IBsaTypes.Remove(type);
                    break;
                case BSA_Collision collision:
                    UndoManager.Instance.AddUndo(new UndoableListRemove<BSA_Collision>(SelectedEntry.SubEntries.CollisionEntries, collision, "BSA Collision Delete"));
                    SelectedEntry.SubEntries.CollisionEntries.Remove(collision);
                    break;
                case BSA_Expiration expiration:
                    UndoManager.Instance.AddUndo(new UndoableListRemove<BSA_Expiration>(SelectedEntry.SubEntries.ExpirationEntries, expiration, "BSA Expiration Delete"));
                    SelectedEntry.SubEntries.ExpirationEntries.Remove(expiration);
                    break;
            }

            SelectedSubtypeRow = null;
            RebuildSubtypeRows();
        }
    }
}
