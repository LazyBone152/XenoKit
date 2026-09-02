using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LB_Common.Forms;
using XenoKit.Engine;
using XenoKit.Engine.Scripting.BSA;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Views
{
    public partial class BsaTab : UserControl, INotifyPropertyChanged
    {
        private void SelectEntry(BSA_Entry entry)
        {
            SelectedEntry = entry;
            entryGrid.SelectedItem = entry;
            entryGrid.ScrollIntoView(entry);
        }

        private void SelectSubtypeSource(object source)
        {
            RebuildSubtypeRows();
            SelectedSubtypeRow = SubtypeRows.FirstOrDefault(row => ReferenceEquals(row.Source, source));

            if (SelectedSubtypeRow != null)
                subtypeGrid.ScrollIntoView(SelectedSubtypeRow);
        }

        private void PlaySelectedEntryPreview()
        {
            BSA_Entry entry = SelectedEntry;
            BSA_File file = GetSelectedFile();

            if (entry == null || file == null || !SceneManager.IsOnTab(EditorTabs.Projectile))
                return;

            if (file.BSA_Entries?.Contains(entry) != true)
                return;

            BsaEffectPreviewController.Instance.Play(entry, files.SelectedMove, file);
        }

        /// <summary>
        /// Mirrors BacTab.EditBacId: rejects a collision rather than silently replacing the other entry.
        /// </summary>
        private void EditBsaId(int newId)
        {
            BSA_File file = GetSelectedFile();
            if (file == null || selectedEntry == null) return;

            if (file.BSA_Entries.Any(entry => entry.SortID == newId && !ReferenceEquals(entry, selectedEntry)))
            {
                NotifyPropertyChanged(nameof(SelectedBsaID));
                MessagePrompt.Show("ID Already Used", "The entered ID is already used by another BSA entry.", MessagePromptButtons.OK, MessagePromptIcon.Warning);
                return;
            }

            int oldId = selectedEntry.SortID;
            UndoManager.Instance.AddUndo(new UndoableProperty<BSA_Entry>(nameof(BSA_Entry.SortID), selectedEntry, oldId, newId, "BSA Entry ID"));
            selectedEntry.SortID = newId;
            NotifyPropertyChanged(nameof(SelectedBsaID));
            RefreshEntryList();
        }

        private void EntryGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                e.Handled = true;
        }

        /// <summary>
        /// Clicking the already selected entry row clears the subtype selection, which brings the entry
        /// sections back into the detail panel.
        /// </summary>
        private void EntryGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = FindParent<DataGridRow>((DependencyObject)e.OriginalSource);

            if (row?.Item is BSA_Entry entry && ReferenceEquals(entry, SelectedEntry))
            {
                SelectedSubtypeRow = null;
                subtypeGrid.SelectedItem = null;
            }
        }
    }
}
