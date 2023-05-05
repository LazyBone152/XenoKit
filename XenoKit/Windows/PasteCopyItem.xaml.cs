using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using XenoKit.Editor;

namespace XenoKit.Windows
{
    /// <summary>
    /// Interaction logic for PasteCopyItem.xaml
    /// </summary>
    public partial class PasteCopyItem : MetroWindow
    {

        private CopyItem copyItem;
        private Move move;
        private BAC_Entry bacEntry = null;
        private bool isBacReplace = false;

        public string DataDescription { get { return copyItem.MainEntriesDetails(); } }
        public string ReferencesDescription { get { return copyItem.ReferencesDetails(); } }
        public string ReferencesCount { get { return copyItem.NumReferences().ToString(); } }
        public bool PasteReferences { get; set; }

        public PasteCopyItem(CopyItem copyItem, Move move)
        {
            PasteReferences = copyItem.MoveGuid != move.MoveGuid;
            this.copyItem = copyItem;
            this.move = move;
            Owner = Application.Current.MainWindow;
            DataContext = this;
            InitializeComponent();
        }

        public PasteCopyItem(CopyItem copyItem, Move move, BAC_Entry bacEntry, bool isBacReplace)
        {
            PasteReferences = copyItem.MoveGuid != move.MoveGuid;
            this.bacEntry = bacEntry;
            this.copyItem = copyItem;
            this.move = move;
            this.isBacReplace = isBacReplace;
            Owner = Application.Current.MainWindow;
            DataContext = this;
            InitializeComponent();

            if (isBacReplace)
            {
                Title = "Replace Entry";
            }
        }

        private void TryAutoResolve()
        {
            if (Xv2CoreLib.Resource.App.SettingsManager.Instance.Settings.XenoKit_AutoResolvePasteReferences)
            {
                Button_Click(null, null);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            List<IUndoRedo> undos = null;

            if (isBacReplace)
            {
                undos = copyItem.PasteIntoMove_Main(move, PasteReferences, bacEntry);
            }
            else if(copyItem.entryType == EntryType.Main)
            {
                undos = copyItem.PasteIntoMove_Main(move, PasteReferences);
            }
            else if (copyItem.entryType == EntryType.Sub)
            {
                if(copyItem.fileType == FileType.Bac)
                {
                    undos = copyItem.PasteIntoMove_Sub(bacEntry, move, PasteReferences);
                }
            }

            UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Paste"));
            Close();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TryAutoResolve();
        }
    }
}
